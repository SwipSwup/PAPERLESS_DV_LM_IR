using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("DocumentLogs")]
    public class DocumentLogEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(255)]
        public string Action { get; set; } = string.Empty; // e.g. "OCR Completed"

        [MaxLength(1000)]
        public string? Details { get; set; }

        [ForeignKey(nameof(DocumentEntity))]
        public int DocumentId { get; set; }

        [Required]
        public DocumentEntity DocumentEntity { get; set; } = null!;
    }
}