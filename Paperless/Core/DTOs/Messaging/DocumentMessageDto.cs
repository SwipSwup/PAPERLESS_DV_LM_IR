namespace Core.DTOs
{
    public class DocumentMessageDto
    {
        public int DocumentId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}