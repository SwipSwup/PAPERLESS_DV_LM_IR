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
            var response = await _client.GetAsync("/api/document");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var documents = await response.Content.ReadFromJsonAsync<List<DocumentDto>>();
            documents.Should().NotBeNull();
        }



        [Fact]
        public async Task CreateDocument_ShouldReturnCreated_WhenFileIsUploaded()
        {
             // Arrange
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
            content.Add(fileContent, "File", "test.pdf");

            // Act
            var response = await _client.PostAsync("/api/document", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdDoc = await response.Content.ReadFromJsonAsync<DocumentDto>();
            createdDoc.Should().NotBeNull();
            createdDoc!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetById_ShouldReturnDocument_WhenDocumentExists()
        {
            // Arrange - Create a doc first
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(new byte[] { 1 }), "File", "get_test.pdf");
            var createResponse = await _client.PostAsync("/api/document", content);
            var createdDoc = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();
            
            // Act
            var response = await _client.GetAsync($"/api/document/{createdDoc!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var fetchedDoc = await response.Content.ReadFromJsonAsync<DocumentDto>();
            fetchedDoc!.Id.Should().Be(createdDoc.Id);
        }

        [Fact]
        public async Task DeleteDocument_ShouldReturnNoContent_WhenDocumentExists()
        {
            // Arrange
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(new byte[] { 1 }), "File", "del_test.pdf");
            var createResponse = await _client.PostAsync("/api/document", content);
            var createdDoc = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

            // Act
            var response = await _client.DeleteAsync($"/api/document/{createdDoc!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            
            // Verify it's gone
            var getResponse = await _client.GetAsync($"/api/document/{createdDoc.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateDocument_ShouldReturnNoContent_WhenValid()
        {
            // Arrange
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(new byte[] { 1 }), "File", "upd_test.pdf");
            var createResponse = await _client.PostAsync("/api/document", content);
            var doc = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

            doc!.FileName = "Updated Name.pdf";

            // Act
            var response = await _client.PutAsJsonAsync($"/api/document/{doc.Id}", doc);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            
            var getResponse = await _client.GetAsync($"/api/document/{doc.Id}");
            var updated = await getResponse.Content.ReadFromJsonAsync<DocumentDto>();
            updated!.FileName.Should().Be("Updated Name.pdf");
        }

        [Fact]
        public async Task Search_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/document/search?keyword=test");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            // We mocked SearchService to return empty list, so this just tests the Controller wiring
            var results = await response.Content.ReadFromJsonAsync<List<DocumentDto>>();
            results.Should().NotBeNull();
        }

        [Fact]
        public async Task AddTag_ShouldReturnOk_WhenValid()
        {
            // Arrange
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(new byte[] { 1 }), "File", "tag_test.pdf");
            var createResponse = await _client.PostAsync("/api/document", content);
            var doc = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

            var tag = new TagDto { Name = "TestTag" };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/document/{doc!.Id}/tags", tag);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RemoveTag_ShouldReturnOk_WhenValid()
        {
            // Arrange
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(new byte[] { 1 }), "File", "rm_tag_test.pdf");
            var createResponse = await _client.PostAsync("/api/document", content);
            var doc = await createResponse.Content.ReadFromJsonAsync<DocumentDto>();

            // Act
            var response = await _client.DeleteAsync($"/api/document/{doc!.Id}/tags/TestTag");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
