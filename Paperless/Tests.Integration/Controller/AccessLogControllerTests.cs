using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Core.DTOs;

namespace Tests.Integration.Controller
{
    public class AccessLogControllerTests : IClassFixture<PaperlessWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AccessLogControllerTests(PaperlessWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/accesslog");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            List<AccessLogDto>? logs = await response.Content.ReadFromJsonAsync<List<AccessLogDto>>();
            logs.Should().NotBeNull();
        }

        // Add more tests if controller has other endpoints like GetByDocumentId
    }
}
