using System;
using System.Collections.Generic;

namespace DAL.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? OcrText { get; set; }   // OCR result (before Elastic indexing)
        public string? Summary { get; set; }   // AI-generated summary
        public DateTime UploadedAt { get; set; }

        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<DocumentLog> Logs { get; set; } = new List<DocumentLog>();
        public ICollection<AccessLog> AccessLogs { get; set; } = new List<AccessLog>();
    }
}