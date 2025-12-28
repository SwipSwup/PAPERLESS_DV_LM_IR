namespace OcrWorker.Services.Tesseract
{
    public interface ITesseractCliRunner
    {
        Task<string> RunOcrAsync(string inputPdfPath, CancellationToken cancellationToken);
        Task<string> RunOcrForImageAsync(byte[] imageBytes, CancellationToken cancellationToken);
    }

}