using OcrWorker.Services.Tesseract;

namespace OcrWorker.Services.Ocr;

public class OcrService(ITesseractCliRunner tesseract) : IOcrService
{
    public async Task<string> ExtractTextFromPdfAsync(string pdfPath, CancellationToken ct)
    {
        return await tesseract.RunOcrAsync(pdfPath, ct);
    }
}