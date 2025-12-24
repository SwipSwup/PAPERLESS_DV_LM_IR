using ceTe.DynamicPDF.Rasterizer;
using Core.DTOs;
using OcrWorker.Messaging;
using OcrWorker.Services.Ocr;
using OcrWorker.Storage;

namespace OcrWorker;

public class Worker(
    ILogger<Worker> logger,
    IMessageConsumer consumer,
    IOcrService ocrService,
    IStorageWrapper minio)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await consumer.ConsumeAsync<DocumentMessageDto>(
            queueName: "documents",
            onMessage: OnMessage,
            stoppingToken);
    }

    private async Task OnMessage(DocumentMessageDto msg, ulong deliveryTag, CancellationToken ct)
    {
        logger.LogInformation("Processing document {id}", msg.DocumentId);

        try
        {
            // Download PDF from MinIO
            string pdf = await minio.DownloadPdfAsync(msg, ct);

            // Run OCR
            string text = await ocrService.ExtractTextFromPdfAsync(pdf, ct);

            // Upload Result
            await minio.UploadTextAsync(msg, text, ct);

            logger.LogInformation("OCR finished for {id}", msg.DocumentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process document {id}", msg.DocumentId);
            // Optionally: nack or dead-letter
        }
    }
}