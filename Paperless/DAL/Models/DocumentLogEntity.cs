namespace DAL.Models
{
    public class DocumentLogEntity
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty; // e.g. "OCR Completed"
        public string? Details { get; set; }

        public int DocumentId { get; set; }
        public DocumentEntity DocumentEntity { get; set; } = null!;
    }
}