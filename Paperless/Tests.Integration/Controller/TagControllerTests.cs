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
            HttpResponseMessage response = await _client.GetAsync("/api/Tag");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            List<TagDto>? tags = await response.Content.ReadFromJsonAsync<List<TagDto>>();
            tags.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateTag_ShouldReturnCreated()
        {
            TagDto tag = new TagDto { Name = "NewTag" };
            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Tag", tag);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            TagDto? created = await response.Content.ReadFromJsonAsync<TagDto>();
            created.Should().NotBeNull();
            created!.Name.Should().Be("NewTag");
        }
    }
}
