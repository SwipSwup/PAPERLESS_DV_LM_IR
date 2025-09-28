namespace Core.Models
{
    public class DocumentLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty; // e.g., "OCR Completed"
        public string? Details { get; set; }
    }
}