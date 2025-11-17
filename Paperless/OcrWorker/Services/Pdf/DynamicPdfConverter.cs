using ceTe.DynamicPDF.Rasterizer;
using OcrWorker.Utils;

namespace OcrWorker.Services.Pdf;

public class DynamicPdfConverter(ITempFileUtility tmpUtility) : IPdfConverter
{
    public Task<string> ConvertToPngFilesAsync(
        string pdfPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string tempDir = tmpUtility.CreateTempDirectory("pdfConverter");
            string tempFile = tmpUtility.CreateTempFile(tempDir, "png");

            PdfRasterizer rasterizer = new PdfRasterizer(pdfPath);

            cancellationToken.ThrowIfCancellationRequested();

            rasterizer.Draw(
                tempFile,
                ImageFormat.Png,
                ImageSize.Dpi300
            );

            return Task.FromResult(tempFile);
        }
        finally
        {
            tmpUtility.DeleteDirectoryAsync(Path.GetDirectoryName(pdfPath)!);
        }
    }
}