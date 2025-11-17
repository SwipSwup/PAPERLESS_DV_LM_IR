namespace OcrWorker.Services.Pdf;

public interface IPdfConverter
{
    Task<string> ConvertToPngFilesAsync(
        string pdfPath,
        CancellationToken cancellationToken = default);
}