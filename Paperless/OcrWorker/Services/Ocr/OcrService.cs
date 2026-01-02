using OcrWorker.Services.Pdf;
using OcrWorker.Services.Tesseract;

namespace OcrWorker.Services.Ocr;

public class OcrService(ITesseractCliRunner tesseract, IPdfConverter pdfConverter, ILogger<OcrService> logger) : IOcrService
{
    public async Task<string> ExtractTextFromPdfAsync(string pdfPath, CancellationToken ct)
    {
        logger.LogInformation("Converting PDF to Image bytes...");
        List<byte[]> pages = await pdfConverter.ConvertToPngBytesAsync(pdfPath, ct);

        System.Text.StringBuilder fullText = new System.Text.StringBuilder();

        for (int i = 0; i < pages.Count; i++)
        {
            logger.LogInformation("Running OCR on page {PageNumber}/{TotalPages}...", i + 1, pages.Count);
            string pageText = await tesseract.RunOcrForImageAsync(pages[i], ct);
            fullText.AppendLine(pageText);
        }

        string result = fullText.ToString();
        logger.LogInformation("OCR Complete. Extracted {Length} characters.", result.Length);
        if (string.IsNullOrWhiteSpace(result))
        {
            logger.LogWarning("OCR Result is empty or whitespace!");
        }
        return result;
    }
}