namespace OcrWorker.Services.Pdf;

public interface IPdfConverter
{
    Task<List<byte[]>> ConvertToPngBytesAsync(
        string pdfPath,
        CancellationToken cancellationToken = default);
}