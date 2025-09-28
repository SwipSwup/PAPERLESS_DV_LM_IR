namespace Core.DTOs
{
    public class DocumentLogDto
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}