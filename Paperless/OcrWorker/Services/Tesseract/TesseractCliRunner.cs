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

        return await ExecuteTesseractAsync(processStartInfo, tempFile, cancellationToken);
    }

    public async Task<string> RunOcrForImageAsync(
        byte[] imageBytes,
        CancellationToken cancellationToken)
    {
        string tempDir = tmpUtility.CreateTempDirectory("tesseract_bytes");
        string tempFile = tmpUtility.CreateTempFile(tempDir, "txt");

        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));

        logger.LogInformation("Starting OCR on memory image ({Length} bytes)", imageBytes.Length);

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = _tesseractExecutablePath,
            // 'stdin' tells Tesseract to read from StandardInput
            Arguments = $"stdin \"{Path.ChangeExtension(tempFile, null)}\" --dpi 300",
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        return await ExecuteTesseractAsync(processStartInfo, tempFile, cancellationToken, imageBytes);
    }

    private async Task<string> ExecuteTesseractAsync(
        ProcessStartInfo startInfo,
        string outputFile,
        CancellationToken cancellationToken,
        byte[]? inputBytes = null)
    {
        using Process process = new Process();
        process.StartInfo = startInfo;
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

            if (inputBytes != null)
            {
                await process.StandardInput.BaseStream.WriteAsync(inputBytes, cancellationToken);
                process.StandardInput.Close();
            }

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
            string result = await File.ReadAllTextAsync(outputFile, cancellationToken);
            return result;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            logger.LogError(ex, "Failed to start Tesseract process. Executable: {Path}", startInfo.FileName);
            throw new InvalidOperationException($"Failed to start Tesseract. Make sure it is installed and '{startInfo.FileName}' is in your PATH. If running in Docker, ensure tesseract-ocr is installed.", ex);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("OCR operation was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during OCR execution. StdErr: {StdErr}",
                string.Join(Environment.NewLine, stdErr));
            throw;
        }
    }
}