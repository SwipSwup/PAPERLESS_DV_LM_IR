using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Core.Configuration;
using Core.DTOs;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using OcrWorker.Utils;

namespace OcrWorker.Storage;

public class StorageWrapper : IStorageWrapper
{
    private readonly ILogger<StorageWrapper> _logger;
    private readonly MinioSettings _settings;
    private readonly IMinioClient _client;
    private readonly ITempFileUtility _tmpUtility;

    public StorageWrapper(
        ILogger<StorageWrapper> logger,
        IOptions<MinioSettings> settings,
        ITempFileUtility tmpUtility)
    {
        _logger = logger;
        _tmpUtility = tmpUtility;
        _settings = settings.Value;

        _client = new MinioClient()
            .WithEndpoint(_settings.Endpoint)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey)
            .WithSSL(false)
            .Build();
    }

    public async Task<string> DownloadPdfAsync(DocumentMessageDto message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message.FileName))
            throw new InvalidOperationException("Document message does not contain a FileName.");

        string objectName = message.FileName;

        // temp folder for this document
        string tempDir = _tmpUtility.CreateTempDirectory("minio");
        string tempFile = _tmpUtility.CreateTempFile(tempDir, objectName);
        
        try
        {
            //await EnsureBucketExistsAsync(ct);

            _logger.LogInformation(
                "Downloading '{object}' from bucket '{bucket}' into '{path}'",
                objectName, _settings.BucketName, tempFile
            );

            GetObjectArgs? args = new GetObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectName)
                .WithCallbackStream(async (stream, token) =>
                {
                    await using FileStream file = File.OpenWrite(tempFile);
                    await stream.CopyToAsync(file, token);
                });

            await _client.GetObjectAsync(args, ct);

            _logger.LogInformation("MinIO download successful: {path}", tempFile);

            return tempFile;
        }
        catch (ObjectNotFoundException)
        {
            _logger.LogError("File '{file}' not found in bucket '{bucket}'", objectName, _settings.BucketName);
            throw;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO error while fetching '{file}'", objectName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching '{file}'", objectName);
            throw;
        }
    }

    public async Task UploadTextAsync(DocumentMessageDto message, string textContent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message.FileName))
            throw new InvalidOperationException("Document message does not contain a FileName.");

        string objectName = Path.ChangeExtension(message.FileName, ".txt");

        try
        {
            //await EnsureBucketExistsAsync(ct);

            _logger.LogInformation(
                "Uploading OCR text for '{object}' to bucket '{bucket}'",
                objectName, _settings.BucketName
            );

            using MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(textContent));
            
            PutObjectArgs args = new PutObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType("text/plain");

            await _client.PutObjectAsync(args, ct);

            _logger.LogInformation("MinIO upload successful: {object}", objectName);
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO error while uploading '{file}'", objectName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while uploading '{file}'", objectName);
            throw;
        }
    }
}