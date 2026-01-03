using ceTe.DynamicPDF.Rasterizer;
using Core.DTOs;
using Core.Models;
using Core.Repositories.Interfaces;
using Core.Messaging;
using OcrWorker.Messaging;
using OcrWorker.Services.Ocr;
using OcrWorker.Storage;

namespace OcrWorker;

public class Worker(
    ILogger<Worker> logger,
    IMessageConsumer consumer,
    IDocumentMessageProducerFactory producerFactory,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Use manual consumer acknowledgment to ensure data safety.
                // Messages are only acknowledged after successful processing to prevent data loss.
                await consumer.ConsumeAsync<DocumentMessageDto>(
                    queueName: "documents",
                    onMessage: OnMessage,
                    stoppingToken);

                // Consumer started successfully, keep alive
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                logger.LogInformation("Worker stopping...");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start consumer. Retrying in 5 seconds...");
                try
                {
                    await Task.Delay(5000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Worker stopping during retry delay...");
                    break;
                }
            }
        }
    }

    private async Task OnMessage(DocumentMessageDto msg, ulong deliveryTag, CancellationToken ct)
    {
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = msg.CorrelationId }))
        {
            logger.LogInformation("Processing document {id} (CorrelationId: {CorrelationId})", msg.DocumentId, msg.CorrelationId);

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                // Resolve scoped services here
                IStorageWrapper minio = scope.ServiceProvider.GetRequiredService<IStorageWrapper>();
                IOcrService ocrService = scope.ServiceProvider.GetRequiredService<IOcrService>();
                IDocumentRepository repo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

                // Download PDF from MinIO
                string pdf = await minio.DownloadPdfAsync(msg, ct);

                // Run OCR with Retry Policy
                string text = string.Empty;
                int retryCount = 0;
                const int MaxRetries = 3;

                while (true)
                {
                    try
                    {
                        text = await ocrService.ExtractTextFromPdfAsync(pdf, ct);
                        break; // Success
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        if (retryCount >= MaxRetries)
                        {
                            logger.LogError(ex, "OCR failed after {Retries} attempts for document {id}", MaxRetries, msg.DocumentId);
                            throw; // Re-throw to trigger NACK/DLQ
                        }

                        int delay = 1000 * (int)Math.Pow(2, retryCount - 1); // 1s, 2s, 4s
                        logger.LogWarning("OCR attempt {Attempt} failed: {Message}. Retrying in {Delay}ms...", retryCount, ex.Message, delay);
                        await Task.Delay(delay, ct);
                    }
                }

                // Upload Result to MinIO
                await minio.UploadTextAsync(msg, text, ct);

                // Update Database
                Document? doc = await repo.GetByIdAsync(msg.DocumentId);

                if (doc != null)
                {
                    doc.OcrText = text;
                    await repo.UpdateAsync(doc);
                    logger.LogInformation("Database updated for document {id}", msg.DocumentId);

                    // Publish to Indexing
                    IDocumentMessageProducer indexingProducer = producerFactory.GetIndexingProducer();
                    await indexingProducer.PublishDocumentAsync(msg);
                    logger.LogInformation("Published document {id} to indexing queue", msg.DocumentId);

                    // Publish to GenAI for summary generation
                    IDocumentMessageProducer genaiProducer = producerFactory.GetGenaiProducer();
                    await genaiProducer.PublishDocumentAsync(msg);
                    logger.LogInformation("Published document {id} to genai queue", msg.DocumentId);
                }
                else
                {
                    logger.LogWarning("Document {id} not found in DB", msg.DocumentId);
                }
            }

            logger.LogInformation("OCR finished for {id}", msg.DocumentId);
        }
    }
}