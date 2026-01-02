using Core.DTOs;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Tests.Integration.Controller
{
    public class TagControllerTests : IClassFixture<PaperlessWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public TagControllerTests(PaperlessWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllTags_ShouldReturnOk()
        {
            var response = await _client.GetAsync("/api/Tag");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>();
            tags.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateTag_ShouldReturnCreated()
        {
            var tag = new TagDto { Name = "NewTag" };
            var response = await _client.PostAsJsonAsync("/api/Tag", tag);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var created = await response.Content.ReadFromJsonAsync<TagDto>();
            created.Should().NotBeNull();
            created!.Name.Should().Be("NewTag");
        }
    }
}
