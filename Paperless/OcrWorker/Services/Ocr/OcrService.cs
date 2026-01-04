using System.Text;
using Core.Exceptions;
using OcrWorker.Services.Pdf;
using OcrWorker.Services.Tesseract;

namespace OcrWorker.Services.Ocr;

public class OcrService(ITesseractCliRunner tesseract, IPdfConverter pdfConverter, ILogger<OcrService> logger)
    : IOcrService
{
    public async Task<string> ExtractTextFromPdfAsync(string pdfPath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
            throw new ArgumentNullException(nameof(pdfPath));

        logger.LogInformation("Starting OCR extraction for file: {PdfPath}", pdfPath);

        try
        {
            logger.LogInformation("Converting PDF to Image bytes...");
            List<byte[]> pages = await pdfConverter.ConvertToPngBytesAsync(pdfPath, ct);

            StringBuilder fullText = new StringBuilder();

            for (int i = 0; i < pages.Count; i++)
            {
                logger.LogDebug("Processing page {PageNumber}/{TotalPages}", i + 1, pages.Count);
                string pageText = await tesseract.RunOcrForImageAsync(pages[i], ct);
                fullText.AppendLine(pageText);
            }

            logger.LogInformation("OCR completed successfully. Total length: {Length}", fullText.Length);

            if (fullText.Length == 0)
            {
                logger.LogWarning("OCR Result is empty or whitespace for file {PdfPath}", pdfPath);
            }

            return fullText.ToString();
        }
        catch (Exception ex) when (ex is not DmsException)
        {
            // Catch-all for Tesseract or System errors
            logger.LogError(ex, "Critical failure in OCR Engine for file {PdfPath}", pdfPath);
            throw new OcrGenerationException("OCR Engine failure", ex, isTransient: true);
        }
    }
}