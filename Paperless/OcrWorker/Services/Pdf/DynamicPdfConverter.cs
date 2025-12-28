// Force update
using ceTe.DynamicPDF.Rasterizer;


namespace OcrWorker.Services.Pdf;

public class DynamicPdfConverter : IPdfConverter
{
    public Task<List<byte[]>> ConvertToPngBytesAsync(
        string pdfPath,
        CancellationToken cancellationToken = default)
    {
        PdfRasterizer rasterizer = new PdfRasterizer(pdfPath);
        List<byte[]> pages = new List<byte[]>();

        for (int i = 0; i < rasterizer.Pages.Count; i++)
        {
            using MemoryStream ms = new MemoryStream();
            rasterizer.Pages[i].Draw(ms, ImageFormat.Png, ImageSize.Dpi300);
            pages.Add(ms.ToArray());
        }

        return Task.FromResult(pages);
    }
}