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
            string prompt = $"Act as a professional document archivist for the 'Paperless DMS' system. " +
                            $"Provide a meaningful and detailed summary of the following document text in 3-5 sentences. " +
                            $"The summary will be used for full-text search and quick user previews. " +
                            $"Focus on accurately capturing the document type, involved parties, key dates, monetary values (if any), and the core subject matter. " +
                            $"Do not use phrases like 'The document contains' or 'Here is a summary'; simply state the facts.\n\n" +
                            $"Text:\n{processedText}";

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
            string prompt = BuildTagGenerationPrompt(processedText);

            GeminiGenerateContentRequest request = CreateRequest(prompt, _settings.Temperature, 200);

            return await ExecuteGeminiRequestAsync(request, response =>
            {
                string? tagsText = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                if (string.IsNullOrWhiteSpace(tagsText))
                {
                   logger.LogWarning("Received empty tags from Gemini API");
                   return Task.FromResult(new List<Tag>());
                }
                
                List<Tag> tags = ParseTagsWithColors(tagsText);
                
                logger.LogInformation("Successfully generated {count} tags", tags.Count);
                return Task.FromResult(tags);
            }, cancellationToken);
        }

        private string BuildTagGenerationPrompt(string text)
        {
            var categories = new[]
            {
                new { Name = "Financial & Money", Colors = new[] { "#f59e0b" /*Amber*/, "#f97316" /*Orange*/ }, Desc = "Invoices, Receipts, Tax, Offers" },
                new { Name = "Legal & Formal", Colors = new[] { "#3b82f6" /*Blue*/, "#6366f1" /*Indigo*/ }, Desc = "Contracts, Agreements, Policy, Official" },
                new { Name = "Urgent & Critical", Colors = new[] { "#ef4444" /*Red*/ }, Desc = "Warnings, Deadlines, Errors, Important" },
                new { Name = "Personal & Private", Colors = new[] { "#8b5cf6" /*Purple*/, "#ec4899" /*Pink*/ }, Desc = "Medical, Family, Identity, Sensitive" },
                new { Name = "Success & Verified", Colors = new[] { "#10b981" /*Green*/, "#14b8a6" /*Teal*/, "#84cc16" /*Lime*/ }, Desc = "Paid, Completed, Certified, Safe" },
                new { Name = "General & Archive", Colors = new[] { "#64748b" /*Slate*/, "#71717a" /*Zinc*/ }, Desc = "Logs, Notes, Drafts, Misc" }
            };

            var colorGuide = string.Join("\n", categories.Select(c => 
                $"   - {c.Name} ({c.Desc}): {string.Join(", ", c.Colors)}"));

            return $"Act as a smart automated filing system. Analyze the following text and generate 3-5 meta-tags. " +
                   $"For each tag, select the most appropriate color based on the semantic meaning of the tag.\n\n" +
                   $"Color Selection Guide:\n" +
                   $"{colorGuide}\n\n" +
                   $"Rules:\n" +
                   $"1. Output format MUST be strictly: TagName|ColorHex (e.g., 'Invoice|#f97316')\n" +
                   $"2. Return one tag per line.\n" +
                   $"3. TagName should be Title Case, 1-3 words.\n" +
                   $"4. Choose the color that best fits the mood or category of the tag.\n" +
                   $"5. No bullets, numbers, or markdown.\n\n" +
                   $"Text:\n{text}";
        }

        // Replaces ParseTagsFromResponse and AssignColorsToTags
        private List<Tag> ParseTagsWithColors(string response)
        {
            var result = new List<Tag>();
            var lines = response.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                try 
                {
                    // Clean the line of bullets/numbers just in case the LLM ignored instructions
                    var cleanLine = System.Text.RegularExpressions.Regex.Replace(line.Trim(), @"^[-•\d.\s]+", "");
                    
                    if (string.IsNullOrWhiteSpace(cleanLine)) continue;

                    var parts = cleanLine.Split('|');
                    string name = parts[0].Trim();
                    string color = parts.Length > 1 ? parts[1].Trim() : Core.Constants.TagPalette.GetRandomColor();

                    // Basic validation
                    if (name.Length > MaxTagLength) name = name.Substring(0, MaxTagLength);

                    result.Add(new Tag { Name = name, Color = color });
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Failed to parse tag line '{line}': {message}", line, ex.Message);
                }
            }

            return result;
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


    }
}

