using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("Documents")]
    public class DocumentEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required] [MaxLength(255)] public string FileName { get; set; } = string.Empty;

        [Required] [MaxLength(500)] public string FilePath { get; set; } = string.Empty;

        public string? OcrText { get; set; } // OCR result (before Elastic indexing)

        public string? Summary { get; set; } // AI-generated summary

        [Required] public DateTime UploadedAt { get; set; }

        public ICollection<TagEntity> Tags { get; set; } = new List<TagEntity>();
        public ICollection<DocumentLogEntity> Logs { get; set; } = new List<DocumentLogEntity>();
        public ICollection<AccessLogEntity> AccessLogs { get; set; } = new List<AccessLogEntity>();

        public long AccessCount { get; set; }
    }
}