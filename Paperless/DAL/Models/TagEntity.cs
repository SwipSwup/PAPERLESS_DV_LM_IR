using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    [Table("Tags")]
    public class TagEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;

        [MaxLength(20)] public string? Color { get; set; }

        public ICollection<DocumentEntity> Documents { get; set; } = new List<DocumentEntity>();
    }
}