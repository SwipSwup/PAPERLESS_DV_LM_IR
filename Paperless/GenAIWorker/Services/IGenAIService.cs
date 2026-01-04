using Core.Models;

namespace GenAIWorker.Services
{
    public interface IGenAiService
    {
        Task<string> GenerateSummaryAsync(string text, CancellationToken cancellationToken = default);
        Task<List<Tag>> GenerateTagsAsync(string text, CancellationToken cancellationToken = default);
    }
}