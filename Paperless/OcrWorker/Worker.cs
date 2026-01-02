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
    OcrWorker.Messaging.IDocumentMessageProducerFactory producerFactory,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await consumer.ConsumeAsync<DocumentMessageDto>(
                    queueName: "documents",
                    onMessage: OnMessage,
                    stoppingToken);

                // Consumer started successfully, keep alive
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start consumer. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task OnMessage(DocumentMessageDto msg, ulong deliveryTag, CancellationToken ct)
    {
        logger.LogInformation("Processing document {id}", msg.DocumentId);

        try
        {
            using (var scope = serviceProvider.CreateScope())
            {
                // Resolve scoped services here
                var minio = scope.ServiceProvider.GetRequiredService<IStorageWrapper>();
                var ocrService = scope.ServiceProvider.GetRequiredService<IOcrService>();
                var repo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

                // Download PDF from MinIO
                string pdf = await minio.DownloadPdfAsync(msg, ct);

                // Run OCR
                string text = await ocrService.ExtractTextFromPdfAsync(pdf, ct);

                // Upload Result to MinIO
                await minio.UploadTextAsync(msg, text, ct);

                // Update Database
                var doc = await repo.GetByIdAsync(msg.DocumentId);

                if (doc != null)
                {
                    doc.OcrText = text;
                    await repo.UpdateAsync(doc);
                    logger.LogInformation("Database updated for document {id}", msg.DocumentId);

                    // Publish to Indexing
                    var indexingProducer = producerFactory.GetIndexingProducer();
                    await indexingProducer.PublishDocumentAsync(msg);
                    logger.LogInformation("Published document {id} to indexing queue", msg.DocumentId);

                    // Publish to GenAI for summary generation
                    var genaiProducer = producerFactory.GetGenaiProducer();
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process document {id}", msg.DocumentId);
            // Optionally: nack or dead-letter
        }
    }
}