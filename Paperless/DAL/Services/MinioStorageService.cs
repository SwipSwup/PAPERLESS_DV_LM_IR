using Core.Configuration;
using Core.Exceptions;
using Core.Interfaces;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace DAL.Services
{
    public class MinioStorageService : IStorageService
    {
        private readonly ILogger<MinioStorageService> _logger;
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;

        public MinioStorageService(MinioSettings settings, ILogger<MinioStorageService> logger)
        {
            _logger = logger;
            _bucketName = settings.BucketName;

            try
            {
                _logger.LogInformation("MinioStorageService: Initializing connection to {Endpoint}", settings.Endpoint);
                _minioClient = new MinioClient()
                    .WithEndpoint(settings.Endpoint)
                    .WithCredentials(settings.AccessKey, settings.SecretKey)
                    .Build();
            }
            catch (Exception ex)
            {
                throw new ServiceException("Failed to initialize MinIO client.", ex);
            }
        }

        private async Task EnsureBucketExistsAsync()
        {
            try
            {
                bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
                if (!found)
                {
                    _logger.LogInformation("MinioStorageService: Bucket '{Bucket}' not found. Creating...", _bucketName);
                    await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
                    _logger.LogInformation("MinioStorageService: Bucket '{Bucket}' created.", _bucketName);
                }
            }
            catch (Exception ex)
            {
                throw new ServiceException($"Failed to ensure bucket '{_bucketName}' exists.", ex);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            await EnsureBucketExistsAsync();

            try
            {
                _logger.LogInformation("MinioStorageService: Uploading '{FileName}' to bucket '{Bucket}'", fileName, _bucketName);

                // Reset stream position if needed
                if (fileStream.Position > 0)
                    fileStream.Position = 0;

                PutObjectArgs? putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs);

                _logger.LogInformation("MinioStorageService: Uploaded '{FileName}' successfully.", fileName);

                // Return the object name (or path)
                return fileName;
            }
            catch (Exception ex)
            {
                throw new ServiceException($"Failed to upload file '{fileName}' to MinIO.", ex);
            }
        }

        public async Task<Stream> GetFileAsync(string filePath)
        {
            await EnsureBucketExistsAsync();

            _logger.LogInformation("MinioStorageService: Retrieving '{FilePath}' from bucket '{Bucket}'", filePath, _bucketName);

            MemoryStream memoryStream = new MemoryStream();

            try
            {
                GetObjectArgs? args = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(filePath)
                    .WithCallbackStream((stream) =>
                    {
                        stream.CopyTo(memoryStream);
                    });

                await _minioClient.GetObjectAsync(args);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                throw new ServiceException($"Failed to retrieve file '{filePath}' from MinIO.", ex);
            }
        }

        public async Task DeleteFileAsync(string filePath)
        {
            await EnsureBucketExistsAsync();
            _logger.LogInformation("MinioStorageService: Deleting '{FilePath}' from bucket '{Bucket}'", filePath, _bucketName);

            try
            {
                RemoveObjectArgs? args = new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(filePath);

                await _minioClient.RemoveObjectAsync(args);
                _logger.LogInformation("MinioStorageService: Deleted '{FilePath}' successfully.", filePath);
            }
            catch (Exception ex)
            {
                throw new ServiceException($"Failed to delete file '{filePath}' from MinIO.", ex);
            }
        }
    }
}
