namespace Core.DTOs
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public DateTime UploadedAt { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}