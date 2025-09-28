namespace Core.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? OcrText { get; set; }
        public string? Summary { get; set; }
        public DateTime UploadedAt { get; set; }

        public List<Tag> Tags { get; set; } = new();
        public List<DocumentLog> Logs { get; set; } = new();
        public List<AccessLog> AccessLogs { get; set; } = new();

        /// <summary>
        /// Logs an access for the given date, increments count if already exists.
        /// </summary>
        public void LogAccess(DateTime date)
        {
            AccessLog? log = AccessLogs.FirstOrDefault(l => l.Date.Date == date.Date);
            if (log != null)
                log.Count++;
            else
                AccessLogs.Add(new AccessLog { Date = date, Count = 1 });
        }

        /// <summary>
        /// Adds a tag to the document if it doesn't already exist.
        /// </summary>
        public void AddTag(string tagName)
        {
            if (!Tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                Tags.Add(new Tag { Name = tagName });
        }

        /// <summary>
        /// Removes a tag from the document if it exists.
        /// </summary>
        public void RemoveTag(string tagName)
        {
            Tag? tag = Tags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
            if (tag != null)
                Tags.Remove(tag);
        }

        /// <summary>
        /// Adds a document log entry.
        /// </summary>
        public void AddLog(string action, string? details = null)
        {
            Logs.Add(new DocumentLog
            {
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}