using System.Diagnostics;
using OcrWorker.Utils;

namespace OcrWorker.Services.Tesseract;

public sealed class TesseractCliRunner(
    ILogger<TesseractCliRunner> logger,
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
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPdfPath);

        logger.LogInformation("Starting OCR on file: {File}", inputPdfPath);

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = _tesseractExecutablePath,
            Arguments = $"\"{inputPdfPath}\" stdout --dpi 300",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        return await ExecuteTesseractAsync(processStartInfo, cancellationToken);
    }

    public async Task<string> RunOcrForImageAsync(
        byte[] imageBytes,
        CancellationToken cancellationToken)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));

        logger.LogInformation("Starting OCR on memory image ({Length} bytes)", imageBytes.Length);

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = _tesseractExecutablePath,
            Arguments = "stdin stdout --dpi 300",
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        return await ExecuteTesseractAsync(processStartInfo, cancellationToken, imageBytes);
    }

    private async Task<string> ExecuteTesseractAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken,
        byte[]? inputBytes = null)
    {
        using Process process = new Process();
        process.StartInfo = startInfo;

        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Failed to start Tesseract process.");

            // Start reading stdout and stderr asynchronously to avoid deadlocks
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            if (inputBytes != null)
            {
                await process.StandardInput.BaseStream.WriteAsync(inputBytes, cancellationToken);
                process.StandardInput.Close();
            }

            await process.WaitForExitAsync(cancellationToken);

            string output = await outputTask;
            string error = await errorTask;

            if (process.ExitCode != 0)
            {
                logger.LogError(
                    "Tesseract failed. Exit code: {ExitCode}, Error: {Error}",
                    process.ExitCode,
                    error
                );

                throw new ApplicationException(
                    $"Tesseract OCR failed with exit code {process.ExitCode}. Error: {error}"
                );
            }

            logger.LogInformation("Tesseract OCR finished successfully.");
            return output;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            logger.LogError(ex, "Failed to start Tesseract process. Executable: {Path}", startInfo.FileName);
            throw new InvalidOperationException(
                $"Failed to start Tesseract. Make sure it is installed and '{startInfo.FileName}' is in your PATH. If running in Docker, ensure tesseract-ocr is installed.",
                ex);
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