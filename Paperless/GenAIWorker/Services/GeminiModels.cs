using System.Text.Json.Serialization;

namespace GenAIWorker.Services
{
    public class GeminiGenerateContentRequest
    {
        [JsonPropertyName("contents")] public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("generationConfig")] public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    public class GeminiContent
    {
        [JsonPropertyName("parts")] public List<GeminiPart> Parts { get; set; } = [];
    }

    public class GeminiPart
    {
        public GeminiPart()
        {
        }

        public GeminiPart(string text)
        {
            Text = text;
        }

        [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    }

    public class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")] public double? Temperature { get; set; }

        [JsonPropertyName("maxOutputTokens")] public int? MaxOutputTokens { get; set; }
    }

    public class GeminiGenerateContentResponse
    {
        [JsonPropertyName("candidates")] public List<GeminiCandidate>? Candidates { get; set; }
    }

    public class GeminiCandidate
    {
        [JsonPropertyName("content")] public GeminiContent? Content { get; set; }
    }

    public class GeminiModelListResponse
    {
        [JsonPropertyName("models")] public List<GeminiModel>? Models { get; set; }
    }

    public class GeminiModel
    {
        [JsonPropertyName("name")] public string? Name { get; set; }

        [JsonPropertyName("supportedGenerationMethods")]
        public List<string>? SupportedGenerationMethods { get; set; }
    }
}