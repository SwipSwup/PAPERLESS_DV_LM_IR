using AutoMapper;
using BL.Services;
using Core.DTOs;
using Core.Models;
using Core.Repositories.Interfaces;
using Moq;

namespace Tests.Unit.Services;

[TestFixture]
public class DocumentServiceTests
{
    private Mock<IDocumentRepository> _mockDocumentRepo;
    private Mock<IAccessLogRepository> _mockAccessLogRepo;
    private Mock<IDocumentLogRepository> _mockDocumentLogRepo;
    private Mock<IMapper> _mockMapper;
    private DocumentService _documentService;

    [SetUp]
    public void Setup()
    {
        _mockDocumentRepo = new Mock<IDocumentRepository>();
        _mockAccessLogRepo = new Mock<IAccessLogRepository>();
        _mockDocumentLogRepo = new Mock<IDocumentLogRepository>();
        _mockMapper = new Mock<IMapper>();
        _documentService = new DocumentService(
            _mockDocumentRepo.Object,
            _mockAccessLogRepo.Object,
            _mockDocumentLogRepo.Object,
            _mockMapper.Object);
    }

    [Test]
    public async Task GetAllDocumentsAsync_ShouldReturnMappedDocuments()
    {
        // Arrange
        var documents = new List<Document>
        {
            new() { Id = 1, FileName = "test1.pdf" },
            new() { Id = 2, FileName = "test2.pdf" }
        };
        var expectedDtos = new List<DocumentDto>
        {
            new() { Id = 1, FileName = "test1.pdf" },
            new() { Id = 2, FileName = "test2.pdf" }
        };

        _mockDocumentRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(documents);
        _mockMapper.Setup(x => x.Map<List<DocumentDto>>(documents)).Returns(expectedDtos);

        // Act
        var result = await _documentService.GetAllDocumentsAsync();

        // Assert
        Assert.That(result, Is.EqualTo(expectedDtos));
        _mockDocumentRepo.Verify(x => x.GetAllAsync(), Times.Once);
        _mockMapper.Verify(x => x.Map<List<DocumentDto>>(documents), Times.Once);
    }

    [Test]
    public async Task GetDocumentByIdAsync_WhenDocumentExists_ShouldReturnMappedDocument()
    {
        // Arrange
        var document = new Document { Id = 1, FileName = "test.pdf" };
        var expectedDto = new DocumentDto { Id = 1, FileName = "test.pdf" };

        _mockDocumentRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(document);
        _mockMapper.Setup(x => x.Map<DocumentDto>(document)).Returns(expectedDto);

        // Act
        var result = await _documentService.GetDocumentByIdAsync(1);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDto));
        _mockDocumentRepo.Verify(x => x.GetByIdAsync(1), Times.Once);
    }

    [Test]
    public async Task GetDocumentByIdAsync_WhenDocumentDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _mockDocumentRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Document?)null);

        // Act
        var result = await _documentService.GetDocumentByIdAsync(1);

        // Assert
        Assert.That(result, Is.Null);
        _mockDocumentRepo.Verify(x => x.GetByIdAsync(1), Times.Once);
        _mockMapper.Verify(x => x.Map<DocumentDto>(It.IsAny<Document>()), Times.Never);
    }

    [Test]
    public async Task AddDocumentAsync_ShouldAddDocumentAndReturnMappedDto()
    {
        // Arrange
        var document = new Document { FileName = "test.pdf" };
        var expectedDto = new DocumentDto { Id = 1, FileName = "test.pdf" };

        _mockDocumentRepo.Setup(x => x.AddAsync(document)).Returns(Task.CompletedTask);
        _mockMapper.Setup(x => x.Map<DocumentDto>(document)).Returns(expectedDto);

        // Act
        var result = await _documentService.AddDocumentAsync(document);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDto));
        _mockDocumentRepo.Verify(x => x.AddAsync(document), Times.Once);
        _mockMapper.Verify(x => x.Map<DocumentDto>(document), Times.Once);
    }

    [Test]
    public async Task UpdateDocumentAsync_ShouldUpdateDocumentAndReturnMappedDto()
    {
        // Arrange
        var document = new Document { Id = 1, FileName = "test.pdf" };
        var expectedDto = new DocumentDto { Id = 1, FileName = "test.pdf" };

        _mockDocumentRepo.Setup(x => x.UpdateAsync(document)).Returns(Task.CompletedTask);
        _mockMapper.Setup(x => x.Map<DocumentDto>(document)).Returns(expectedDto);

        // Act
        var result = await _documentService.UpdateDocumentAsync(document);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDto));
        _mockDocumentRepo.Verify(x => x.UpdateAsync(document), Times.Once);
        _mockMapper.Verify(x => x.Map<DocumentDto>(document), Times.Once);
    }

    [Test]
    public async Task DeleteDocumentAsync_WhenDocumentExists_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var document = new Document { Id = 1, FileName = "test.pdf" };
        _mockDocumentRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(document);
        _mockDocumentRepo.Setup(x => x.DeleteAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _documentService.DeleteDocumentAsync(1);

        // Assert
        Assert.That(result, Is.True);
        _mockDocumentRepo.Verify(x => x.GetByIdAsync(1), Times.Once);
        _mockDocumentRepo.Verify(x => x.DeleteAsync(1), Times.Once);
    }

    [Test]
    public async Task DeleteDocumentAsync_WhenDocumentDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        _mockDocumentRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Document?)null);

        // Act
        var result = await _documentService.DeleteDocumentAsync(1);

        // Assert
        Assert.That(result, Is.False);
        _mockDocumentRepo.Verify(x => x.GetByIdAsync(1), Times.Once);
        _mockDocumentRepo.Verify(x => x.DeleteAsync(1), Times.Never);
    }

    [Test]
    public async Task SearchDocumentsAsync_ShouldReturnMappedDocuments()
    {
        // Arrange
        var keyword = "test";
        var documents = new List<Document>
        {
            new() { Id = 1, FileName = "test1.pdf" }
        };
        var expectedDtos = new List<DocumentDto>
        {
            new() { Id = 1, FileName = "test1.pdf" }
        };

        _mockDocumentRepo.Setup(x => x.SearchDocumentsAsync(keyword)).ReturnsAsync(documents);
        _mockMapper.Setup(x => x.Map<List<DocumentDto>>(documents)).Returns(expectedDtos);

        // Act
        var result = await _documentService.SearchDocumentsAsync(keyword);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDtos));
        _mockDocumentRepo.Verify(x => x.SearchDocumentsAsync(keyword), Times.Once);
        _mockMapper.Verify(x => x.Map<List<DocumentDto>>(documents), Times.Once);
    }

    [Test]
    public async Task LogAccessAsync_WhenLogExists_ShouldIncrementCount()
    {
        // Arrange
        var documentId = 1;
        var date = DateTime.Today;
        var existingLog = new AccessLog { Id = 1, Date = date, Count = 1 };
        var logs = new List<AccessLog> { existingLog };

        _mockAccessLogRepo.Setup(x => x.GetByDocumentIdAsync(documentId)).ReturnsAsync(logs);
        _mockAccessLogRepo.Setup(x => x.UpdateAsync(It.IsAny<AccessLog>())).Returns(Task.CompletedTask);

        // Act
        await _documentService.LogAccessAsync(documentId, date);

        // Assert
        Assert.That(existingLog.Count, Is.EqualTo(2));
        _mockAccessLogRepo.Verify(x => x.GetByDocumentIdAsync(documentId), Times.Once);
        _mockAccessLogRepo.Verify(x => x.UpdateAsync(existingLog), Times.Once);
    }

    [Test]
    public async Task LogAccessAsync_WhenLogDoesNotExist_ShouldCreateNewLog()
    {
        // Arrange
        var documentId = 1;
        var date = DateTime.Today;
        var logs = new List<AccessLog>();

        _mockAccessLogRepo.Setup(x => x.GetByDocumentIdAsync(documentId)).ReturnsAsync(logs);
        _mockAccessLogRepo.Setup(x => x.AddAsync(It.IsAny<AccessLog>())).Returns(Task.CompletedTask);

        // Act
        await _documentService.LogAccessAsync(documentId, date);

        // Assert
        _mockAccessLogRepo.Verify(x => x.GetByDocumentIdAsync(documentId), Times.Once);
        _mockAccessLogRepo.Verify(x => x.AddAsync(It.Is<AccessLog>(log => 
            log.Id == documentId && log.Date == date && log.Count == 1)), Times.Once);
    }

    [Test]
    public async Task AddLogToDocumentAsync_ShouldAddDocumentLog()
    {
        // Arrange
        var documentId = 1;
        var action = "OCR Completed";
        var details = "Text extracted successfully";

        _mockDocumentLogRepo.Setup(x => x.AddAsync(It.IsAny<DocumentLog>())).Returns(Task.CompletedTask);

        // Act
        await _documentService.AddLogToDocumentAsync(documentId, action, details);

        // Assert
        _mockDocumentLogRepo.Verify(x => x.AddAsync(It.Is<DocumentLog>(log => 
            log.Id == documentId && log.Action == action && log.Details == details)), Times.Once);
    }

    [Test]
    public async Task AddTagToDocumentAsync_WhenDocumentExistsAndTagNotExists_ShouldAddTag()
    {
        // Arrange
        var documentId = 1;
        var tagName = "Important";
        var document = new Document { Id = documentId, Tags = new List<Tag>() };

        _mockDocumentRepo.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync(document);
        _mockDocumentRepo.Setup(x => x.UpdateAsync(document)).Returns(Task.CompletedTask);

        // Act
        await _documentService.AddTagToDocumentAsync(documentId, tagName);

        // Assert
        Assert.That(document.Tags.Count, Is.EqualTo(1));
        Assert.That(document.Tags.First().Name, Is.EqualTo(tagName));
        _mockDocumentRepo.Verify(x => x.GetByIdAsync(documentId), Times.Once);
        _mockDocumentRepo.Verify(x => x.UpdateAsync(document), Times.Once);
    }

    [Test]
    public async Task AddTagToDocumentAsync_WhenDocumentDoesNotExist_ShouldNotAddTag()
    {
        // Arrange
        var documentId = 1;
        var tagName = "Important";

        _mockDocumentRepo.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync((Document?)null);

        // Act
        await _documentService.AddTagToDocumentAsync(documentId, tagName);

        // Assert
        _mockDocumentRepo.Verify(x => x.GetByIdAsync(documentId), Times.Once);
        _mockDocumentRepo.Verify(x => x.UpdateAsync(It.IsAny<Document>()), Times.Never);
    }

    [Test]
    public async Task RemoveTagFromDocumentAsync_WhenDocumentAndTagExist_ShouldRemoveTag()
    {
        // Arrange
        var documentId = 1;
        var tagName = "Important";
        var tag = new Tag { Name = tagName };
        var document = new Document { Id = documentId, Tags = new List<Tag> { tag } };

        _mockDocumentRepo.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync(document);
        _mockDocumentRepo.Setup(x => x.UpdateAsync(document)).Returns(Task.CompletedTask);

        // Act
        await _documentService.RemoveTagFromDocumentAsync(documentId, tagName);

        // Assert
        Assert.That(document.Tags.Count, Is.EqualTo(0));
        _mockDocumentRepo.Verify(x => x.GetByIdAsync(documentId), Times.Once);
        _mockDocumentRepo.Verify(x => x.UpdateAsync(document), Times.Once);
    }
}
