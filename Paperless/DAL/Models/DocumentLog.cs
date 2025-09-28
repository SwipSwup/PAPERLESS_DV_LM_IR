using System;

namespace DAL.Models
{
    public class DocumentLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty; // e.g. "OCR Completed"
        public string? Details { get; set; }

        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;
    }
}