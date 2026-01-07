using System.Net;
using System.Net.Http.Json;
using Core.DTOs;
using FluentAssertions;
using Xunit;

namespace Tests.Integration.Controller
{
    public class DocumentControllerTests : IClassFixture<PaperlessWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public DocumentControllerTests(PaperlessWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetDocuments_ShouldReturnList()
        {
            // Act
            HttpResponseMessage response = await _client.GetAsync("/api/document");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            List<DocumentDto>? documents = await response.Content.ReadFromJsonAsync<List<DocumentDto>>();
            documents.Should().NotBeNull();
        }



        [Fact]
        public async Task CreateDocument_ShouldReturnCreated_WhenFileIsUploaded()
        {
            // Arrange
            MultipartFormDataContent content = new MultipartFormDataContent();
            ByteArrayContent fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
            content.Add(fileContent, "File", "test.pdf");

            // Act
            HttpResponseMessage response = await _client.PostAsync("/api/document", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            DocumentDto? createdDoc = await response.Content.ReadFromJsonAsync<DocumentDto>();
            createdDoc.Should().NotBeNull();
            createdDoc!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetById_ShouldReturnDocument_WhenDocumentExists()
        {
            // Arrange - Create a doc first
            MultipartFormDataContent content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(new byte[] { 1 }), "File", "get_test.pdf");
            HttpResponseMessage createResponse = await _client.PostAsync("/api/document", content);
            DocumentDto? createdDoc = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

            // Act
            HttpResponseMessage response = await _client.GetAsync($"/api/document/{createdDoc!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            DocumentDto? fetchedDoc = await response.Content.ReadFromJsonAsync<DocumentDto>();
            fetchedDoc!.Id.Should().Be(createdDoc.Id);
        }

        [Fact]
        public async Task UpdateDocument_ShouldReturnNoContent_WhenValid()
        {
            // Arrange
            MultipartFormDataContent content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(new byte[] { 1 }), "File", "upd_test.pdf");
            HttpResponseMessage createResponse = await _client.PostAsync("/api/document", content);
            DocumentDto? doc = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

            doc!.FileName = "Updated Name.pdf";

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/document/{doc.Id}", doc);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            HttpResponseMessage getResponse = await _client.GetAsync($"/api/document/{doc.Id}");
            DocumentDto? updated = await getResponse.Content.ReadFromJsonAsync<DocumentDto>();
            updated!.FileName.Should().Be("Updated Name.pdf");
        }

        [Fact]
        public async Task Search_ShouldReturnOk()
        {
            // Act
            HttpResponseMessage response = await _client.GetAsync("/api/document/search?keyword=test");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            // We mocked SearchService to return empty list, so this just tests the Controller wiring
            List<DocumentDto>? results = await response.Content.ReadFromJsonAsync<List<DocumentDto>>();
            results.Should().NotBeNull();
        }

        [Fact]
        public async Task AddTag_ShouldReturnOk_WhenValid()
        {
            // Arrange
            MultipartFormDataContent content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(new byte[] { 1 }), "File", "tag_test.pdf");
            HttpResponseMessage createResponse = await _client.PostAsync("/api/document", content);
            DocumentDto? doc = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

            TagDto tag = new TagDto { Name = "TestTag" };

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/document/{doc!.Id}/tags", tag);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RemoveTag_ShouldReturnOk_WhenValid()
        {
            // Arrange
            MultipartFormDataContent content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(new byte[] { 1 }), "File", "rm_tag_test.pdf");
            HttpResponseMessage createResponse = await _client.PostAsync("/api/document", content);
            DocumentDto? doc = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

            // Act
            HttpResponseMessage response = await _client.DeleteAsync($"/api/document/{doc!.Id}/tags/TestTag");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
