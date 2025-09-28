namespace DAL.Models
{
    public class TagEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<DocumentEntity> Documents { get; set; } = new List<DocumentEntity>();
    }
}