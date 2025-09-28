namespace DAL.Models
{
    public class DocumentEntity
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? OcrText { get; set; }   // OCR result (before Elastic indexing)
        public string? Summary { get; set; }   // AI-generated summary
        public DateTime UploadedAt { get; set; }

        public ICollection<TagEntity> Tags { get; set; } = new List<TagEntity>();
        public ICollection<DocumentLogEntity> Logs { get; set; } = new List<DocumentLogEntity>();
        public ICollection<AccessLogEntity> AccessLogs { get; set; } = new List<AccessLogEntity>();
    }
}