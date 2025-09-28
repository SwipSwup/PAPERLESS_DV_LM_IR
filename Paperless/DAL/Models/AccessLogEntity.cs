namespace DAL.Models
{
    public class AccessLogEntity
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } // daily access date
        public int Count { get; set; }     // how many times accessed
        public int DocumentId { get; set; }
        public DocumentEntity DocumentEntity { get; set; } = null!;
    }
}