using Core.Interfaces;
using Core.DTOs;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace BL.Services
{
    public class SearchService(
        ElasticsearchClient elasticClient,
        ILogger<SearchService> logger) : ISearchService
    {
        private const string IndexName = "documents";

        public async Task<IEnumerable<DocumentDto>> SearchDocumentsAsync(string searchTerm)
        {
            try
            {
                SearchResponse<DocumentDto> response = await elasticClient.SearchAsync<DocumentDto>(s => s
                    .Index(IndexName)
                    .Query(q => q
                        .Bool(b => b
                            .Should(
                                s => s.Match(m => m
                                    .Field(d => d.OcrText)
                                    .Query(searchTerm)
                                    .Fuzziness(new Fuzziness("2"))
                                ),
                                s => s.Match(m => m
                                    .Field(d => d.FileName)
                                    .Query(searchTerm)
                                    .Fuzziness(new Fuzziness("AUTO"))
                                ),
                                s => s.Match(m => m
                                    .Field(d => d.Summary)
                                    .Query(searchTerm)
                                    .Fuzziness(new Fuzziness("AUTO"))
                                )
                            )
                        )
                    )
                );

                if (!response.IsValidResponse)
                {
                    logger.LogError("Search query failed: {Debug}", response.DebugInformation);
                    return Enumerable.Empty<DocumentDto>();
                }

                // Log success debug info to check what query was actually sent
                logger.LogInformation("Search Debug: {Debug}", response.DebugInformation);

                return response.Documents;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error searching documents");
                return Enumerable.Empty<DocumentDto>();
            }
        }
    }
}
