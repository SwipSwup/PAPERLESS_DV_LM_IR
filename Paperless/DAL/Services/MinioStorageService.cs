using Core.Configuration;
using Core.Exceptions;
using Core.Interfaces;
using log4net;
using Minio;
using Minio.DataModel.Args;
using System.Reflection;

namespace DAL.Services
{
    public class MinioStorageService : IStorageService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;

        public MinioStorageService(MinioSettings settings)
        {
            _bucketName = settings.BucketName;
            
            try 
            {
                log.Info($"MinioStorageService: Initializing connection to {settings.Endpoint}");
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
                    log.Info($"MinioStorageService: Bucket '{_bucketName}' not found. Creating...");
                    await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
                    log.Info($"MinioStorageService: Bucket '{_bucketName}' created.");
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
                log.Info($"MinioStorageService: Uploading '{fileName}' to bucket '{_bucketName}'");
                
                // Reset stream position if needed
                if (fileStream.Position > 0)
                    fileStream.Position = 0;

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs);
                
                log.Info($"MinioStorageService: Uploaded '{fileName}' successfully.");
                
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
            
            log.Info($"MinioStorageService: Retrieving '{filePath}' from bucket '{_bucketName}'");
            
            var memoryStream = new MemoryStream();
            
            try
            {
                var args = new GetObjectArgs()
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
             log.Info($"MinioStorageService: Deleting '{filePath}' from bucket '{_bucketName}'");

             try
             {
                 var args = new RemoveObjectArgs()
                     .WithBucket(_bucketName)
                     .WithObject(filePath);

                 await _minioClient.RemoveObjectAsync(args);
                 log.Info($"MinioStorageService: Deleted '{filePath}' successfully.");
             }
             catch (Exception ex)
             {
                 throw new ServiceException($"Failed to delete file '{filePath}' from MinIO.", ex);
             }
        }
    }
}
