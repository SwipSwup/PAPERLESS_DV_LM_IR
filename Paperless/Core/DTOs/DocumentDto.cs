namespace Core.DTOs
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public DateTime UploadedAt { get; set; }
        public long AccessCount { get; set; }
        public string? OcrText { get; set; }
        public List<TagDto> Tags { get; set; } = new();
    }
}