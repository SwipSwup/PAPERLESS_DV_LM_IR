using System.Collections.Generic;

namespace DAL.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}