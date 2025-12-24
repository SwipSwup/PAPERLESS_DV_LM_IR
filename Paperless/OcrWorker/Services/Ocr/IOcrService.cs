namespace OcrWorker.Services.Ocr;

public interface IOcrService
{
    Task<string> ExtractTextFromPdfAsync(string pdfPath, CancellationToken ct);
}