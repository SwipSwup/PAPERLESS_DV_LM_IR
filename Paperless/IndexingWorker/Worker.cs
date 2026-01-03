using Elastic.Clients.Elasticsearch;
using Core.DTOs;
using Core.Repositories.Interfaces;
using IndexingWorker.Messaging;
using Microsoft.Extensions.Options;
using Core.Configuration;

namespace IndexingWorker;

public class Worker(
    ILogger<Worker> logger,
    IMessageConsumer consumer,
    ElasticsearchClient elasticClient,
    IOptions<RabbitMqSettings> rabbitMqOptions,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private const string IndexName = "documents";
    private readonly RabbitMqSettings _rabbitSettings = rabbitMqOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureIndexExistsAsync(stoppingToken);
        
        var queueName = _rabbitSettings.QueueName ?? "indexing";
        logger.LogInformation("IndexingWorker starting. Consuming from queue: {queue}", queueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await consumer.ConsumeAsync<DocumentMessageDto>(
                    queueName: queueName, 
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
        try 
        {
            var existsResponse = await elasticClient.Indices.ExistsAsync(IndexName, ct);
            if (!existsResponse.Exists)
            {
                var response = await elasticClient.Indices.CreateAsync(IndexName, c => c
                    .Mappings(m => m
                        .Properties<Core.DTOs.DocumentDto>(p => p
                            .Text(d => d.OcrText)
                            .Text(d => d.FileName)
                            .Text(d => d.Summary)
                        )
                    ), ct);
                    
                if (response.IsValidResponse)
                {
                    logger.LogInformation("Created index {index}", IndexName);
                }
                else
                {
                    logger.LogError("Failed to create index {index}: {debug}", IndexName, response.DebugInformation);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking/creating index {index}", IndexName);
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
