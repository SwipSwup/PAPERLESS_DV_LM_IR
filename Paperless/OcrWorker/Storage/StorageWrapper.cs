using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OcrWorker.Config;
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
                objectName, _settings.Bucket, tempFile
            );

            GetObjectArgs? args = new GetObjectArgs()
                .WithBucket(_settings.Bucket)
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
            _logger.LogError("File '{file}' not found in bucket '{bucket}'", objectName, _settings.Bucket);
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
}