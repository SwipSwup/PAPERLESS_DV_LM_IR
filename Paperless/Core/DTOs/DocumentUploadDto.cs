using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs
{
    public class DocumentUploadDto
    {
        [Required] public IFormFile File { get; set; } = null!;

        public string? Title { get; set; }

        public string? Category { get; set; }

        public List<string>? Tags { get; set; }
    }
}