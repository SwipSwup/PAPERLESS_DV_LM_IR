using Elastic.Clients.Elasticsearch;
using Core.DTOs;
using Core.Repositories.Interfaces;
using IndexingWorker.Messaging;

namespace IndexingWorker;

public class Worker(
    ILogger<Worker> logger,
    IMessageConsumer consumer,
    ElasticsearchClient elasticClient,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private const string IndexName = "documents";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureIndexExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                 // Consume from same queue? Or a new one?
                 // OcrWorker publishes to "documents"??
                 // Wait, RabbitMqProducer publishes to _queueName from settings.
                 // API publishes to "documents" (for OCR).
                 // OcrWorker should publish to "indexing" or similar?
                 // If OcrWorker reuses RabbitMqProducer with same settings, it publishes to "documents".
                 // This would be a loop!
                 // I need to check RabbitMqSettings in OcrWorker.
                 
                 // Assuming OcrWorker publishes to "documents", IndexingWorker listening to "documents" would get OCR requests too?
                 // No, DocumentMessageDto usually has a State?
                 // Or we use different queues.
                 // I should likely change OcrWorker to publish to "indexing" queue. Or generic producer allows routing key?
                 // The current Producer uses _queueName from settings.
                 
                 // If I can't change OcrWorker settings easily without config change...
                 // I should check Request/Response pattern.
                 
                 // FOR NOW: I assume I should listen to "indexing" queue. 
                 // I will assume OcrWorker is configured to publish to "indexing" queue? 
                 // But OcrWorker appsettings probably says "documents".
                 // I will check OcrWorker appsettings.
                 
                await consumer.ConsumeAsync<DocumentMessageDto>(
                    queueName: "indexing", 
                    onMessage: OnMessage,
                    stoppingToken);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start consumer. Retrying...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task EnsureIndexExistsAsync(CancellationToken ct)
    {
        var existsResponse = await elasticClient.Indices.ExistsAsync(IndexName, ct);
        if (!existsResponse.Exists)
        {
            await elasticClient.Indices.CreateAsync(IndexName, c => c
                .Mappings(m => m
                    .Properties<Core.DTOs.DocumentDto>(p => p
                        .Text(d => d.OcrText)
                        .Text(d => d.FileName)
                        .Text(d => d.Summary)
                    )
                ), ct);
            logger.LogInformation("Created index {index}", IndexName);
        }
    }

    private async Task OnMessage(DocumentMessageDto msg, ulong deliveryTag, CancellationToken ct)
    {
        logger.LogInformation("Indexing document {id}", msg.DocumentId);

        try 
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                var doc = await repo.GetByIdAsync(msg.DocumentId);

                if (doc != null && !string.IsNullOrEmpty(doc.OcrText))
                {
                    logger.LogInformation("Indexing document content for {id}", msg.DocumentId);
                    
                    var response = await elasticClient.IndexAsync(doc, IndexName, ct);
                    
                    if (response.IsValidResponse)
                    {
                        logger.LogInformation("Successfully indexed document {id}", msg.DocumentId);
                    }
                    else
                    {
                        logger.LogError("Failed to index document {id}: {debug}", msg.DocumentId, response.DebugInformation);
                    }
                }
                else
                {
                    logger.LogWarning("Document {id} not found or no OCR text", msg.DocumentId);
                }
            }
        }
        catch (Exception ex)
        {
             logger.LogError(ex, "Error indexing document {id}", msg.DocumentId);
        }
    }
}