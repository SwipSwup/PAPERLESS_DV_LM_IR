using System.Net.Http.Json;
using System.Text.Json;
using Core.Configuration;
using Core.Exceptions;
using Core.Models;
using Microsoft.Extensions.Options;

namespace GenAIWorker.Services
{
    public class GenAIService : IGenAIService
    {
        private readonly GenAISettings _settings;
        private readonly ILogger<GenAIService> _logger;
        private readonly HttpClient _httpClient;

        public GenAIService(
            IOptions<GenAISettings> settings,
            ILogger<GenAIService> logger,
            HttpClient httpClient)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<string> GenerateSummaryAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Empty text provided for summary generation");
                throw new ArgumentException("Text cannot be empty", nameof(text));
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogError("GenAI API key is not configured");
                throw new ServiceException("GenAI API key is not configured");
            }

            try
            {
                _logger.LogInformation("Generating summary for text of length {length}", text.Length);

                // Truncate text if too long (Gemini has token limits)
                const int maxTextLength = 30000;
                string processedText = text.Length > maxTextLength 
                    ? text.Substring(0, maxTextLength) + "... [truncated]"
                    : text;

                var prompt = $"Please provide a concise summary of the following document text in 2-3 sentences:\n\n{processedText}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = _settings.Temperature,
                        maxOutputTokens = _settings.MaxTokens
                    }
                };

                // First, try to list available models to see what the API key has access to
                string? availableModel = null;
                try
                {
                    var listUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={_settings.ApiKey}";
                    var listResponse = await _httpClient.GetAsync(listUrl, cancellationToken);
                    if (listResponse.IsSuccessStatusCode)
                    {
                        var listContent = await listResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                        if (listContent.TryGetProperty("models", out var models))
                        {
                            foreach (var model in models.EnumerateArray())
                            {
                                if (model.TryGetProperty("name", out var name))
                                {
                                    var modelName = name.GetString();
                                    _logger.LogInformation("Found available model: {model}", modelName);
                                    
                                    // Look for a flash or pro model that supports generateContent
                                    if (modelName != null && 
                                        (modelName.Contains("flash") || modelName.Contains("pro")) &&
                                        modelName.Contains("gemini"))
                                    {
                                        // Check if it supports generateContent
                                        if (model.TryGetProperty("supportedGenerationMethods", out var methods))
                                        {
                                            foreach (var method in methods.EnumerateArray())
                                            {
                                                if (method.GetString() == "generateContent")
                                                {
                                                    availableModel = modelName.Replace("models/", "");
                                                    _logger.LogInformation("Using model: {model}", availableModel);
                                                    break;
                                                }
                                            }
                                        }
                                        if (availableModel != null) break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not list available models, using configured model");
                }

                // Use available model or fall back to configured model
                string modelToUse = availableModel ?? _settings.Model;
                string baseUrl = "https://generativelanguage.googleapis.com/v1beta";
                
                var url = $"{baseUrl}/models/{modelToUse}:generateContent?key={_settings.ApiKey}";
                
                _logger.LogInformation("Calling Gemini API with model {model} at {baseUrl}", modelToUse, baseUrl);

                var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Gemini API returned error: {statusCode} - {error}", 
                        response.StatusCode, errorContent);
                    throw new ServiceException($"Failed to generate summary: {response.StatusCode} - {errorContent}");
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                
                if (jsonResponse.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var summary = parts[0].GetProperty("text").GetString();
                        
                        if (string.IsNullOrWhiteSpace(summary))
                        {
                            _logger.LogWarning("Received empty summary from Gemini API");
                            throw new ServiceException("Received empty summary from GenAI service");
                        }

                        _logger.LogInformation("Successfully generated summary of length {length}", summary.Length);
                        return summary;
                    }
                }

                _logger.LogError("Unexpected response format from Gemini API");
                throw new ServiceException("Unexpected response format from GenAI service");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while calling Gemini API");
                throw new ServiceException("Network error while calling GenAI service", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout while calling Gemini API");
                throw new ServiceException("Timeout while calling GenAI service", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini API response");
                throw new ServiceException("Failed to parse GenAI service response", ex);
            }
            catch (Exception ex) when (!(ex is ServiceException))
            {
                _logger.LogError(ex, "Unexpected error while generating summary");
                throw new ServiceException("Unexpected error while generating summary", ex);
            }
        }

        public async Task<List<Tag>> GenerateTagsAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Empty text provided for tag generation");
                return new List<Tag>();
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogError("GenAI API key is not configured");
                throw new ServiceException("GenAI API key is not configured");
            }

            try
            {
                _logger.LogInformation("Generating tags for text of length {length}", text.Length);

                // Truncate text if too long (Gemini has token limits)
                const int maxTextLength = 30000;
                string processedText = text.Length > maxTextLength 
                    ? text.Substring(0, maxTextLength) + "... [truncated]"
                    : text;

                var prompt = $"Based on the following document text, generate 3-5 relevant tags that categorize this document. " +
                             $"Return only the tag names, one per line, without numbers or bullets. " +
                             $"Tags should be concise (1-3 words), relevant to the content, and useful for organization. " +
                             $"Examples: 'Invoice', 'Contract', 'Receipt', 'Medical Record', 'Legal Document', 'Report', etc.\n\n" +
                             $"Document text:\n{processedText}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = _settings.Temperature,
                        maxOutputTokens = 200
                    }
                };

                // Get available model (reuse logic from GenerateSummaryAsync)
                string? availableModel = null;
                try
                {
                    var listUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={_settings.ApiKey}";
                    var listResponse = await _httpClient.GetAsync(listUrl, cancellationToken);
                    if (listResponse.IsSuccessStatusCode)
                    {
                        var listContent = await listResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                        if (listContent.TryGetProperty("models", out var models))
                        {
                            foreach (var model in models.EnumerateArray())
                            {
                                if (model.TryGetProperty("name", out var name))
                                {
                                    var modelName = name.GetString();
                                    if (modelName != null && 
                                        (modelName.Contains("flash") || modelName.Contains("pro")) &&
                                        modelName.Contains("gemini"))
                                    {
                                        if (model.TryGetProperty("supportedGenerationMethods", out var methods))
                                        {
                                            foreach (var method in methods.EnumerateArray())
                                            {
                                                if (method.GetString() == "generateContent")
                                                {
                                                    availableModel = modelName.Replace("models/", "");
                                                    break;
                                                }
                                            }
                                        }
                                        if (availableModel != null) break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not list available models, using configured model");
                }

                string modelToUse = availableModel ?? _settings.Model;
                string baseUrl = "https://generativelanguage.googleapis.com/v1beta";
                
                var url = $"{baseUrl}/models/{modelToUse}:generateContent?key={_settings.ApiKey}";
                
                _logger.LogInformation("Calling Gemini API for tag generation with model {model}", modelToUse);

                var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Gemini API returned error for tag generation: {statusCode} - {error}", 
                        response.StatusCode, errorContent);
                    throw new ServiceException($"Failed to generate tags: {response.StatusCode} - {errorContent}");
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                
                if (jsonResponse.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var tagsText = parts[0].GetProperty("text").GetString();
                        
                        if (string.IsNullOrWhiteSpace(tagsText))
                        {
                            _logger.LogWarning("Received empty tags from Gemini API");
                            return new List<Tag>();
                        }

                        // Parse tags from response (one per line)
                        var tags = ParseTagsFromResponse(tagsText);
                        
                        // Assign colors to tags
                        var tagsWithColors = AssignColorsToTags(tags);
                        
                        _logger.LogInformation("Successfully generated {count} tags", tagsWithColors.Count);
                        return tagsWithColors;
                    }
                }

                _logger.LogError("Unexpected response format from Gemini API for tag generation");
                throw new ServiceException("Unexpected response format from GenAI service");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while calling Gemini API for tag generation");
                throw new ServiceException("Network error while calling GenAI service", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout while calling Gemini API for tag generation");
                throw new ServiceException("Timeout while calling GenAI service", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini API response for tag generation");
                throw new ServiceException("Failed to parse GenAI service response", ex);
            }
            catch (Exception ex) when (!(ex is ServiceException))
            {
                _logger.LogError(ex, "Unexpected error while generating tags");
                throw new ServiceException("Unexpected error while generating tags", ex);
            }
        }

        private List<string> ParseTagsFromResponse(string response)
        {
            var tags = new List<string>();
            var lines = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Remove common prefixes like "- ", "• ", numbers, etc.
                trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^[-•\d.\s]+", "");
                trimmed = trimmed.Trim();
                
                if (!string.IsNullOrWhiteSpace(trimmed) && trimmed.Length <= 100) // Max tag name length
                {
                    tags.Add(trimmed);
                }
            }
            
            return tags;
        }

        private List<Tag> AssignColorsToTags(List<string> tagNames)
        {
            // Available colors from the UI
            var colors = new[]
            {
                "#3b82f6", // blue
                "#ef4444", // red
                "#10b981", // green
                "#f59e0b", // amber
                "#8b5cf6", // purple
                "#ec4899", // pink
                "#6366f1", // indigo
                "#14b8a6", // teal
                "#84cc16", // lime
                "#f97316", // orange
                "#64748b", // slate
                "#71717a"  // zinc
            };

            var random = new Random();
            var tags = new List<Tag>();
            
            foreach (var tagName in tagNames)
            {
                // Assign a random color to each tag
                var color = colors[random.Next(colors.Length)];
                tags.Add(new Tag 
                { 
                    Name = tagName, 
                    Color = color 
                });
            }
            
            return tags;
        }
    }
}

