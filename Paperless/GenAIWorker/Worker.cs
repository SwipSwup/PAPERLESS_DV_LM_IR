using Core.DTOs;
using Core.Models;
using Core.Repositories.Interfaces;
using Core.Exceptions;
using Core.Messaging;
using GenAIWorker.Messaging;
using GenAIWorker.Services;

namespace GenAIWorker
{
    public class Worker(
        ILogger<Worker> logger,
        IMessageConsumer consumer,
        IGenAIService genAiService,
        IDocumentMessageProducer producer,
        IServiceProvider serviceProvider)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("Starting GenAI worker consumer...");

                    await consumer.ConsumeAsync<DocumentMessageDto>(
                        queueName: "genai",
                        onMessage: OnMessage,
                        stoppingToken);

                    // Consumer started successfully, keep alive
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("GenAI Worker stopping...");
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
                        logger.LogInformation("GenAI Worker stopping during retry delay...");
                        break;
                    }
                }
            }
        }

        private async Task OnMessage(DocumentMessageDto msg, ulong deliveryTag, CancellationToken ct)
        {
            logger.LogInformation("Processing GenAI summary and tags for document {id}", msg.DocumentId);

            using IServiceScope scope = serviceProvider.CreateScope();
            IDocumentRepository repo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

            // Get document with OCR text
            Document? doc = await repo.GetByIdAsync(msg.DocumentId);

            if (doc == null)
            {
                logger.LogWarning("Document {id} not found in database", msg.DocumentId);
                return;
            }

            if (string.IsNullOrWhiteSpace(doc.OcrText))
            {
                logger.LogWarning("Document {id} has no OCR text available for summary generation", msg.DocumentId);
                return;
            }

            bool needsUpdate = false;

            // Generate summary if it doesn't exist
            if (string.IsNullOrWhiteSpace(doc.Summary))
            {
                logger.LogInformation("Generating summary for document {id} with OCR text length {length}",
                    msg.DocumentId, doc.OcrText.Length);

                try
                {
                    string summary = await genAiService.GenerateSummaryAsync(doc.OcrText, ct);
                    doc.Summary = summary;
                    needsUpdate = true;
                    logger.LogInformation("Successfully generated summary for document {id}", msg.DocumentId);
                }
                catch (ServiceException ex)
                {
                    logger.LogError(ex, "Failed to generate summary for document {id}: {message}",
                        msg.DocumentId, ex.Message);

                    // If the service indicates a permanent error (like 404 Model Not Found), 
                    // we should not retry indefinitely.
                    if (ex.Message.Contains("404") || ex.Message.Contains("NotFound") || ex.Message.Contains("not found"))
                    {
                         logger.LogError("Permanent error encountered (404/NotFound). Skipping document {id} to avoid poison message loop.", msg.DocumentId);
                         // Do not rethrow, just let it proceed (maybe to tags, or finish)
                         // Since summary failed permanently, we can't do much.
                    }
                    else
                    {
                        throw; // Re-throw to trigger nack for transient errors
                    }
                }
            }
            else
            {
                logger.LogInformation("Document {id} already has a summary, skipping summary generation", msg.DocumentId);
            }

            // Generate tags if document has no tags or only a few tags
            // We'll generate tags even if some exist, but only add new ones
            logger.LogInformation("Generating tags for document {id}", msg.DocumentId);

            try
            {
                List<Tag> generatedTags = await genAiService.GenerateTagsAsync(doc.OcrText, ct);

                if (generatedTags.Any())
                {
                    // Add only tags that don't already exist (case-insensitive comparison)
                    int addedCount = 0;
                    foreach (Tag newTag in generatedTags)
                    {
                        if (!doc.Tags.Any(t => t.Name.Equals(newTag.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            doc.Tags.Add(newTag);
                            addedCount++;
                            logger.LogInformation("Added tag '{tagName}' with color {color} to document {id}",
                                newTag.Name, newTag.Color, msg.DocumentId);
                        }
                        else
                        {
                            logger.LogInformation("Tag '{tagName}' already exists on document {id}, skipping",
                                newTag.Name, msg.DocumentId);
                        }
                    }

                    if (addedCount > 0)
                    {
                        needsUpdate = true;
                        logger.LogInformation("Successfully added {count} new tags to document {id}",
                            addedCount, msg.DocumentId);
                    }
                    else
                    {
                        logger.LogInformation("All generated tags already exist on document {id}", msg.DocumentId);
                    }
                }
            }
            catch (ServiceException ex)
            {
                if (ex.Message.Contains("404") || ex.Message.Contains("NotFound") || ex.Message.Contains("not found"))
                {
                    logger.LogWarning("Tag generation failed with 404 (Model not found). Skipping tags for document {id}.", msg.DocumentId);
                }
                else
                {
                    logger.LogError(ex, "Failed to generate tags for document {id}: {message}",
                        msg.DocumentId, ex.Message);
                }
                // Don't throw - tag generation failure shouldn't prevent summary from being saved
                // But if summary was generated, we should still save it
            }

            // Update document if we made any changes
            if (needsUpdate)
            {
                await repo.UpdateAsync(doc);
                logger.LogInformation("Successfully updated document {id}", msg.DocumentId);

                // Trigger re-indexing so the new summary is searchable
                await producer.PublishDocumentAsync(msg);
                logger.LogInformation("Triggered re-indexing for document {id}", msg.DocumentId);
            }
        }
    }
}
