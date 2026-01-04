using System.Net.Http.Json;
using System.Text.Json;
using Core.Configuration;
using Core.Exceptions;
using Core.Models;
using Microsoft.Extensions.Options;

namespace GenAIWorker.Services
{
    public partial class GenAiService(
        IOptions<GenAiSettings> settings,
        ILogger<GenAiService> logger,
        HttpClient httpClient)
        : IGenAiService
    {
        private readonly GenAiSettings _settings = settings.Value;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1";
        private const int MaxTextLength = 30000;
        private const int MaxTagLength = 100;

        public async Task<string> GenerateSummaryAsync(string text, CancellationToken cancellationToken = default)
        {
            ValidateInput(text);

            logger.LogInformation("Generating summary for text of length {length}", text.Length);

            string processedText = TruncateText(text);
            string prompt = $"Act as a professional document archivist for the 'Paperless DMS' system. " +
                            $"Provide a concise, high-density summary of the following document text. " +
                            $"The summary will be used for quick user previews in a list view.\n" +
                            $"Rules:\n" +
                            $"1. The summary MUST be exactly 2 to 3 sentences long.\n" +
                            $"2. Prioritize key facts: document type (invoice, contract, etc.), specific parties involved, dates, and monetary amounts.\n" +
                            $"3. Start directly with the main subject. Do not use intro phrases like 'This document is'.\n" +
                            $"4. Use professional, objective language.\n\n" +
                            $"Text:\n{processedText}";

            GeminiGenerateContentRequest request = CreateRequest(prompt, _settings.Temperature, 2000);

            return await ExecuteGeminiRequestAsync(request, response =>
            {
                var parts = response.Candidates?.FirstOrDefault()?.Content?.Parts;
                string summary = parts != null 
                    ? string.Join("", parts.Select(p => p.Text)) 
                    : string.Empty;

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

            GeminiGenerateContentRequest request = CreateRequest(prompt, _settings.Temperature, 2000);

            return await ExecuteGeminiRequestAsync(request, response =>
            {
                var parts = response.Candidates?.FirstOrDefault()?.Content?.Parts;
                string tagsText = parts != null 
                    ? string.Join("", parts.Select(p => p.Text)) 
                    : string.Empty;
                
                // Debug log to see exactly what the model returned
                logger.LogInformation("GenAI Raw Tag Response: {response}", tagsText);

                if (string.IsNullOrWhiteSpace(tagsText))
                {
                    logger.LogWarning("Received empty tags from Gemini API");
                    return Task.FromResult(new List<Tag>());
                }

                List<Tag> tags = ParseTagsFromJson(tagsText);

                logger.LogInformation("Successfully generated {count} tags", tags.Count);
                return Task.FromResult(tags);
            }, cancellationToken);
        }

        private string BuildTagGenerationPrompt(string text)
        {
            var categories = new[]
            {
                new
                {
                    Name = "Financial & Money", Colors = new[] { "#f59e0b" /*Amber*/, "#f97316" /*Orange*/ },
                    Desc = "Invoices, Receipts, Tax, Offers"
                },
                new
                {
                    Name = "Legal & Formal", Colors = new[] { "#3b82f6" /*Blue*/, "#6366f1" /*Indigo*/ },
                    Desc = "Contracts, Agreements, Policy, Official"
                },
                new
                {
                    Name = "Urgent & Critical", Colors = new[] { "#ef4444" /*Red*/ },
                    Desc = "Warnings, Deadlines, Errors, Important"
                },
                new
                {
                    Name = "Personal & Private", Colors = new[] { "#8b5cf6" /*Purple*/, "#ec4899" /*Pink*/ },
                    Desc = "Medical, Family, Identity, Sensitive"
                },
                new
                {
                    Name = "Success & Verified",
                    Colors = new[] { "#10b981" /*Green*/, "#14b8a6" /*Teal*/, "#84cc16" /*Lime*/ },
                    Desc = "Paid, Completed, Certified, Safe"
                },
                new
                {
                    Name = "General & Archive", Colors = new[] { "#64748b" /*Slate*/, "#71717a" /*Zinc*/ },
                    Desc = "Logs, Notes, Drafts, Misc"
                }
            };

            string colorGuide = string.Join("\n", categories.Select(c =>
                $"   - {c.Name} ({c.Desc}): {string.Join(", ", c.Colors)}"));

            return $"Act as a smart automated filing system. Analyze the following text and generate a JSON array of 2 to 4 distinct meta-tags. " +
                   $"Tags should categorize the document for efficient retrieval.\n\n" +
                   $"Color Selection Guide:\n" +
                   $"{colorGuide}\n\n" +
                   $"Rules:\n" +
                   $"1. Output MUST be a valid JSON array of objects with 'name' and 'color' properties.\n" +
                   $"2. Generate exactly 2 to 4 tags.\n" +
                   $"3. Tag Strategy:\n" +
                   $"   - Tag 1: Broad Category (e.g., Financial, Legal, Personal, Technic).\n" +
                   $"   - Tag 2: Specific Type (e.g., Invoice, Contract, Prescription, Manual).\n" +
                   $"   - Tag 3-4 (Optional): Key Entity or Topic (e.g., Employer Name, 'Tax 2024', 'Warranty').\n" +
                   $"4. Tag names should be Title Case, short (1-3 words) and visually clean.\n" +
                   $"5. Select colors that intuitively match the tag meaning (e.g., Red for Urgent, Green for Success).\n" +
                   $"6. Do NOT wrap the JSON in markdown.\n\n" +
                   $"Example Output:\n" +
                   $"[\n" +
                   $"  {{ \"name\": \"Financial\", \"color\": \"#f59e0b\" }},\n" +
                   $"  {{ \"name\": \"Invoice\", \"color\": \"#f97316\" }},\n" +
                   $"  {{ \"name\": \"Amazone\", \"color\": \"#64748b\" }}\n" +
                   $"]\n\n" +
                   $"Text:\n{text}";
        }

        private List<Tag> ParseTagsFromJson(string response)
        {
            try
            {
                // Remove markdown code blocks if present
                string json = response.Replace("```json", "").Replace("```", "").Trim();
                
                var tags = JsonSerializer.Deserialize<List<TagDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (tags == null) return [];

                return tags.Select(t => new Tag 
                { 
                    Name = t.Name?.Length > MaxTagLength ? t.Name[..MaxTagLength] : t.Name ?? "Unknown", 
                    Color = t.Color ?? Core.Constants.TagPalette.GetRandomColor() 
                }).ToList();
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to parse JSON tags: {message}. Raw response: {response}", ex.Message, response);
                return [];
            }
        }
        
        private class TagDto
        {
            public string? Name { get; set; }
            public string? Color { get; set; }
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
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
                return;

            logger.LogError("GenAI API key is not configured");
            throw new ServiceException("GenAI API key is not configured");
        }

        private string TruncateText(string text)
        {
            return text.Length > MaxTextLength
                ? string.Concat(text.AsSpan(0, MaxTextLength), "... [truncated]")
                : text;
        }

        private GeminiGenerateContentRequest CreateRequest(string prompt, double? temperature, int? maxTokens)
        {
            return new GeminiGenerateContentRequest
            {
                Contents =
                [
                    new GeminiContent
                    {
                        Parts = [new GeminiPart(text: prompt)]
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
                    logger.LogError("Gemini API returned error: {statusCode} - {error}", response.StatusCode,
                        errorContent);
                    throw new ServiceException($"Failed to call GenAI service: {response.StatusCode} - {errorContent}");
                }

                GeminiGenerateContentResponse? jsonResponse =
                    await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(
                        cancellationToken: cancellationToken);

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
                    GeminiModelListResponse? modelList =
                        await listResponse.Content.ReadFromJsonAsync<GeminiModelListResponse>(
                            cancellationToken: cancellationToken);

                    if (modelList?.Models != null)
                    {
                        List<string?> modelNames = modelList.Models.Select(m => m.Name).ToList();
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

                        logger.LogWarning(
                            "No suitable Gemini model found in list. Falling back to configured model.");
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

        [System.Text.RegularExpressions.GeneratedRegex(@"^[-•\d.\s]+")]
        private static partial System.Text.RegularExpressions.Regex MyRegex();
    }
}