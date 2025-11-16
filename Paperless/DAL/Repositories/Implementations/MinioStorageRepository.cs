using Core.Models;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using log4net;
using System.Reflection;

namespace DAL.Repositories.Implementations
{
    public class MinioDocumentRepository
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly PaperlessDBContext _context;
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;

        public MinioDocumentRepository(PaperlessDBContext context, IMinioClient minioClient, string bucketName)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
            _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
        }

        private async Task EnsureBucketExistsAsync()
        {
            bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
            if (!found)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
            }
        }

        public async Task<Document> UploadAsync(string filePath, int userId)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            await EnsureBucketExistsAsync();

            string fileName = Path.GetFileName(filePath);
            string objectName = $"{userId}/{Guid.NewGuid()}_{fileName}";

            log.Info($"Uploading file {fileName} as object {objectName} for user {userId}...");

            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithFileName(filePath));

            // Speichern in DB
            var entity = new DocumentEntity
            {
                FileName = fileName,
                FilePath = objectName,
                UploadedAt = DateTime.UtcNow
            };

            _context.Documents.Add(entity);
            await _context.SaveChangesAsync();

            return new Document
            {
                Id = entity.Id,
                FileName = entity.FileName,
                FilePath = entity.FilePath,
                UploadedAt = entity.UploadedAt
            };
        }

        public async Task DownloadAsync(Document document, string destinationPath)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrEmpty(destinationPath))
                throw new ArgumentNullException(nameof(destinationPath));

            log.Info($"Downloading object {document.FilePath} to {destinationPath}...");

            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(document.FilePath)
                .WithCallbackStream(stream =>
                {
                    using var fileStream = File.Create(destinationPath);
                    stream.CopyTo(fileStream);
                }));
        }

        public async Task DeleteAsync(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            log.Info($"Deleting object {document.FilePath}...");

            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(document.FilePath));

            var entity = await _context.Documents.FirstOrDefaultAsync(d => d.Id == document.Id);
            if (entity != null)
            {
                _context.Documents.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
