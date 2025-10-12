using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("AccessLogs")]
    public class AccessLogEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } // daily access date

        [Required]
        [Range(0, int.MaxValue)]
        public int Count { get; set; } // how many times accessed

        [ForeignKey(nameof(DocumentEntity))]
        public int DocumentId { get; set; }

        [Required]
        public DocumentEntity DocumentEntity { get; set; } = null!;
    }
}