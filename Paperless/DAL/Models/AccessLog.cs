using System;

namespace DAL.Models
{
    public class AccessLog
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } // daily access date
        public int Count { get; set; }     // how many times accessed

        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;
    }
}