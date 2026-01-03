using System.Net.Http.Json;
using System.Text.Json;
using Core.Configuration;
using Core.Exceptions;
using Core.Models;
using Microsoft.Extensions.Options;

namespace GenAIWorker.Services
{
    public class GenAiService(
        IOptions<GenAISettings> settings,
        ILogger<GenAiService> logger,
        HttpClient httpClient)
        : IGenAIService
    {
        private readonly GenAISettings _settings = settings.Value;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1";
        private const int MaxTextLength = 30000;
        private const int MaxTagLength = 100;

        public async Task<string> GenerateSummaryAsync(string text, CancellationToken cancellationToken = default)
        {
            ValidateInput(text);

            logger.LogInformation("Generating summary for text of length {length}", text.Length);

            string processedText = TruncateText(text);
            string prompt = $"Please provide a concise summary of the following document text in 2-3 sentences:\n\n{processedText}";

            GeminiGenerateContentRequest request = CreateRequest(prompt, _settings.Temperature, _settings.MaxTokens);
            
            return await ExecuteGeminiRequestAsync(request, response =>
            {
                string? summary = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                if (string.IsNullOrWhiteSpace(summary))
                {
                    throw new ServiceException("Received empty summary from GenAI service");
                }
                logger.LogInformation("Successfully generated summary of length {length}", summary.Length);
                return Task.FromResult(summary);
            }, cancellationToken);
        }

        public async Task<List<Tag>> GenerateTagsAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning("Empty text provided for tag generation");
                return new List<Tag>();
            }

            ValidateApiKey();

            logger.LogInformation("Generating tags for text of length {length}", text.Length);

            string processedText = TruncateText(text);
            string prompt = $"Based on the following document text, generate 3-5 relevant tags that categorize this document. " +
                            $"Return only the tag names, one per line, without numbers or bullets. " +
                            $"Tags should be concise (1-3 words), relevant to the content, and useful for organization. " +
                            $"Examples: 'Invoice', 'Contract', 'Receipt', 'Medical Record', 'Legal Document', 'Report', etc.\n\n" +
                            $"Document text:\n{processedText}";

            GeminiGenerateContentRequest request = CreateRequest(prompt, _settings.Temperature, 200);

            return await ExecuteGeminiRequestAsync(request, response =>
            {
                string? tagsText = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                if (string.IsNullOrWhiteSpace(tagsText))
                {
                   logger.LogWarning("Received empty tags from Gemini API");
                   return Task.FromResult(new List<Tag>());
                }
                
                List<string> tags = ParseTagsFromResponse(tagsText);
                List<Tag> tagsWithColors = AssignColorsToTags(tags);
                
                logger.LogInformation("Successfully generated {count} tags", tagsWithColors.Count);
                return Task.FromResult(tagsWithColors);
            }, cancellationToken);
        }

        private void ValidateInput(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning("Empty text provided for generation");
                throw new ArgumentException("Text cannot be empty", nameof(text));
            }
            ValidateApiKey();
        }

        private void ValidateApiKey()
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                logger.LogError("GenAI API key is not configured");
                throw new ServiceException("GenAI API key is not configured");
            }
        }

        private string TruncateText(string text)
        {
            return text.Length > MaxTextLength
                ? text.Substring(0, MaxTextLength) + "... [truncated]"
                : text;
        }

        private GeminiGenerateContentRequest CreateRequest(string prompt, double? temperature, int? maxTokens)
        {
            return new GeminiGenerateContentRequest
            {
                Contents =
                [
                    new()
                    {
                        Parts = [new() { Text = prompt }]
                    }
                ],
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = temperature,
                    MaxOutputTokens = maxTokens
                }
            };
        }

        private async Task<T> ExecuteGeminiRequestAsync<T>(
            GeminiGenerateContentRequest request, 
            Func<GeminiGenerateContentResponse, Task<T>> processResponse,
            CancellationToken cancellationToken)
        {
            try
            {
                string modelToUse = await GetBestAvailableModelAsync(cancellationToken);
                string url = $"{BaseUrl}/models/{modelToUse}:generateContent?key={_settings.ApiKey}";

                logger.LogInformation("Calling Gemini API with model {model}", modelToUse);

                HttpResponseMessage response = await httpClient.PostAsJsonAsync(url, request, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogError("Gemini API returned error: {statusCode} - {error}", response.StatusCode, errorContent);
                    throw new ServiceException($"Failed to call GenAI service: {response.StatusCode} - {errorContent}");
                }

                GeminiGenerateContentResponse? jsonResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(cancellationToken: cancellationToken);
                
                if (jsonResponse == null)
                {
                     throw new ServiceException("Received null response from GenAI service");
                }

                return await processResponse(jsonResponse);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Network error while calling Gemini API");
                throw new ServiceException("Network error while calling GenAI service", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                logger.LogError(ex, "Timeout while calling Gemini API");
                throw new ServiceException("Timeout while calling GenAI service", ex);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to parse Gemini API response");
                throw new ServiceException("Failed to parse GenAI service response", ex);
            }
            catch (Exception ex) when (ex is not ServiceException)
            {
                 logger.LogError(ex, "Unexpected error in GenAIService");
                 throw new ServiceException("Unexpected error in GenAI service", ex);
            }
        }

        private async Task<string> GetBestAvailableModelAsync(CancellationToken cancellationToken)
        {
             try
             {
                 string listUrl = $"{BaseUrl}/models?key={_settings.ApiKey}";
                 HttpResponseMessage listResponse = await httpClient.GetAsync(listUrl, cancellationToken);
                 
                 if (listResponse.IsSuccessStatusCode)
                 {
                     GeminiModelListResponse? modelList = await listResponse.Content.ReadFromJsonAsync<GeminiModelListResponse>(cancellationToken: cancellationToken);
                     
                     if (modelList?.Models != null)
                     {
                         var modelNames = modelList.Models.Select(m => m.Name).ToList();
                         logger.LogInformation("Available GenAI models: {models}", string.Join(", ", modelNames));

                         GeminiModel? preferredModel = modelList.Models
                             .FirstOrDefault(m => 
                                 m.Name != null &&
                                 m.Name.Contains("gemini") &&
                                 (m.Name.Contains("flash") || m.Name.Contains("pro")) &&
                                 m.SupportedGenerationMethods != null &&
                                 m.SupportedGenerationMethods.Contains("generateContent"));

                         if (preferredModel?.Name != null)
                         {
                             string modelName = preferredModel.Name.Replace("models/", "");
                             logger.LogInformation("Discovered preferred model: {model}", modelName);
                             return modelName;
                         }
                         else 
                         {
                             logger.LogWarning("No suitable Gemini model found in list. Falling back to configured model.");
                         }
                     }
                 }
                 else
                 {
                      logger.LogWarning("Failed to list models. Status: {status}", listResponse.StatusCode);
                 }
             }
             catch (Exception ex)
             {
                 logger.LogError(ex, "Could not list available models, falling back to configured model");
             }

             return _settings.Model;
        }

        private List<string> ParseTagsFromResponse(string response)
        {
            return response.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Select(line => System.Text.RegularExpressions.Regex.Replace(line, @"^[-•\d.\s]+", ""))
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line) && line.Length <= MaxTagLength)
                .ToList();
        }

        private List<Tag> AssignColorsToTags(List<string> tagNames)
        {
            return tagNames.Select(tagName => new Tag
            {
                Name = tagName,
                Color = Core.Constants.TagPalette.GetRandomColor()
            }).ToList();
        }
    }
}

