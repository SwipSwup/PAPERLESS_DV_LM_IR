using System.Diagnostics;
using OcrWorker.Services.Tesseract;
using OcrWorker.Utils;

namespace PaperlessServices.OcrWorker.Ocr;

public sealed class TesseractCliRunner(
    ILogger<TesseractCliRunner> logger,
    ITempFileUtility tmpUtility,
    IConfiguration configuration)
    : ITesseractCliRunner
{
    private readonly string _tesseractExecutablePath =
        configuration["Tesseract:ExecutablePath"]
        ?? throw new InvalidOperationException(
            "Tesseract executable path not configured."
            );

    public async Task<string> RunOcrAsync(
        string inputPdfPath,
        CancellationToken cancellationToken)
    {
        string tempDir = tmpUtility.CreateTempDirectory("tesseract");
        string tempFile = tmpUtility.CreateTempFile(tempDir, "txt");

        ArgumentException.ThrowIfNullOrWhiteSpace(inputPdfPath);

        logger.LogInformation("Starting OCR on file: {File}", inputPdfPath);

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = _tesseractExecutablePath,
            Arguments = $"\"{inputPdfPath}\" \"{Path.ChangeExtension(tempFile, null)}\" --dpi 300",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new Process();
        process.StartInfo = processStartInfo;
        process.EnableRaisingEvents = true;

        List<string> stdOut = [];
        List<string> stdErr = [];

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                stdOut.Add(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                stdErr.Add(e.Data);
        };

        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Failed to start Tesseract process.");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                logger.LogError(
                    "Tesseract failed. Exit code: {ExitCode}, Error: {Error}",
                    process.ExitCode,
                    string.Join(Environment.NewLine, stdErr)
                );

                throw new ApplicationException(
                    $"Tesseract OCR failed with exit code {process.ExitCode}. Error: {string.Join(", ", stdErr)}"
                );
            }

            logger.LogInformation("Tesseract OCR finished successfully.");

            // Tesseract automatically writes output to {output}.txt
            string result = await File.ReadAllTextAsync(tempFile, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("OCR operation was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during OCR execution.");
            throw;
        }
    }
}