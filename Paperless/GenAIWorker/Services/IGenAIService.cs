using Core.Models;

namespace GenAIWorker.Services
{
    public interface IGenAIService
    {
        Task<string> GenerateSummaryAsync(string text, CancellationToken cancellationToken = default);
        Task<List<Tag>> GenerateTagsAsync(string text, CancellationToken cancellationToken = default);
    }
}

