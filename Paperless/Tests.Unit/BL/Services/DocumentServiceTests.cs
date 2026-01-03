using AutoMapper;
using BL.Services;
using Core.DTOs;
using Core.Messaging;
using Core.Models;
using Core.Models;
using Core.Repositories.Interfaces;
using Core.Interfaces;
using Moq;
using Microsoft.Extensions.Logging;

namespace Tests.Unit.BL.Services;

[TestFixture]
public class DocumentServiceTests
{
    private Mock<IDocumentRepository> _mockDocumentRepo;
    private Mock<IAccessLogRepository> _mockAccessLogRepo;
    private Mock<IDocumentLogRepository> _mockDocumentLogRepo;
    private Mock<IDocumentMessageProducer> _mockDocumentMessageProducer;
    private Mock<ISearchService> _mockSearchService;
    private Mock<IMapper> _mockMapper;
    private Mock<ILogger<DocumentService>> _mockLogger;
    private DocumentService _documentService;

    [SetUp]
    public void Setup()
    {
        _mockDocumentRepo = new Mock<IDocumentRepository>();
        _mockAccessLogRepo = new Mock<IAccessLogRepository>();
        _mockDocumentLogRepo = new Mock<IDocumentLogRepository>();
        _mockDocumentMessageProducer = new Mock<IDocumentMessageProducer>();
        _mockSearchService = new Mock<ISearchService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<DocumentService>>();
        _documentService = new DocumentService(
            _mockDocumentRepo.Object,
            _mockAccessLogRepo.Object,
            _mockDocumentLogRepo.Object,
            _mockMapper.Object,
            _mockDocumentMessageProducer.Object,
            _mockSearchService.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task GetAllDocumentsAsync_ShouldReturnMappedDocuments()
    {
        // Arrange
        List<Document> documents = new List<Document>
        {
            new() { Id = 1, FileName = "test1.pdf" },
            new() { Id = 2, FileName = "test2.pdf" }
        };
        List<DocumentDto> expectedDtos = new List<DocumentDto>
        {
            new() { Id = 1, FileName = "test1.pdf" },
            new() { Id = 2, FileName = "test2.pdf" }
        };

        _mockDocumentRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(documents);
        _mockMapper.Setup(x => x.Map<List<DocumentDto>>(documents)).Returns(expectedDtos);

        // Act
        List<DocumentDto> result = await _documentService.GetAllDocumentsAsync();

        // Assert
        Assert.That(result, Is.EqualTo(expectedDtos));
        _mockDocumentRepo.Verify(x => x.GetAllAsync(), Times.Once);
        _mockMapper.Verify(x => x.Map<List<DocumentDto>>(documents), Times.Once);
    }

    [Test]
    public async Task GetDocumentByIdAsync_WhenDocumentExists_ShouldReturnMappedDocument()
    {
        // Arrange
        Document document = new Document { Id = 1, FileName = "test.pdf" };
        DocumentDto expectedDto = new DocumentDto { Id = 1, FileName = "test.pdf" };

        _mockDocumentRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(document);
        _mockMapper.Setup(x => x.Map<DocumentDto>(document)).Returns(expectedDto);

        // Act
        DocumentDto? result = await _documentService.GetDocumentByIdAsync(1);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDto));
        _mockDocumentRepo.Verify(x => x.GetByIdAsync(1), Times.Once);
    }


    [Test]
    public async Task GetDocumentByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        _mockDocumentRepo.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Document?)null);
        DocumentDto? result = await _documentService.GetDocumentByIdAsync(99);
        Assert.IsNull(result);
    }

    [Test]
    public async Task UpdateDocumentAsync_ShouldNotThrow_WhenNotFound()
    {
        Document doc = new Document { Id = 99 };
        _mockDocumentRepo.Setup(x => x.UpdateAsync(doc)).Returns(Task.CompletedTask);
        // Assuming service doesn't throw if update passes
        await _documentService.UpdateDocumentAsync(doc);
        _mockDocumentRepo.Verify(x => x.UpdateAsync(doc), Times.Once);
    }

    [Test]
    public async Task AddDocumentAsync_ShouldAddDocumentAndReturnMappedDto()
    {
        // Arrange
        Document document = new Document { FileName = "test.pdf" };
        DocumentDto expectedDto = new DocumentDto { Id = 1, FileName = "test.pdf" };

        _mockDocumentRepo.Setup(x => x.AddAsync(document)).Returns(Task.CompletedTask);
        _mockMapper.Setup(x => x.Map<DocumentDto>(document)).Returns(expectedDto);

        // Act
        DocumentDto result = await _documentService.AddDocumentAsync(document);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDto));
        _mockDocumentRepo.Verify(x => x.AddAsync(document), Times.Once);
        _mockMapper.Verify(x => x.Map<DocumentDto>(document), Times.Once);
    }

    [Test]
    public async Task UpdateDocumentAsync_ShouldUpdateDocumentAndReturnMappedDto()
    {
        // Arrange
        Document document = new Document { Id = 1, FileName = "test.pdf" };
        DocumentDto expectedDto = new DocumentDto { Id = 1, FileName = "test.pdf" };

        _mockDocumentRepo.Setup(x => x.UpdateAsync(document)).Returns(Task.CompletedTask);
        _mockMapper.Setup(x => x.Map<DocumentDto>(document)).Returns(expectedDto);

        // Act
        DocumentDto result = await _documentService.UpdateDocumentAsync(document);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDto));
        _mockDocumentRepo.Verify(x => x.UpdateAsync(document), Times.Once);
        _mockMapper.Verify(x => x.Map<DocumentDto>(document), Times.Once);
    }

    [Test]
    public async Task DeleteDocumentAsync_WhenDocumentDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        _mockDocumentRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Document?)null);

        // Act
        bool result = await _documentService.DeleteDocumentAsync(1);

        // Assert
        Assert.That(result, Is.False);
        _mockDocumentRepo.Verify(x => x.GetByIdAsync(1), Times.Once);
        _mockDocumentRepo.Verify(x => x.DeleteAsync(1), Times.Never);
    }

    [Test]
    public async Task SearchDocumentsAsync_ShouldReturnMappedDocuments()
    {
        // Arrange
        string keyword = "test";
        List<DocumentDto> expectedDtos = new List<DocumentDto>
        {
            new() { Id = 1, FileName = "test1.pdf" }
        };

        _mockSearchService.Setup(x => x.SearchDocumentsAsync(keyword)).ReturnsAsync(expectedDtos);

        // Act
        List<DocumentDto> result = await _documentService.SearchDocumentsAsync(keyword);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDtos));
        _mockSearchService.Verify(x => x.SearchDocumentsAsync(keyword), Times.Once);
        _mockDocumentRepo.Verify(x => x.SearchDocumentsAsync(It.IsAny<string>()), Times.Never);
        _mockMapper.Verify(x => x.Map<List<DocumentDto>>(It.IsAny<object>()), Times.Never);
    }

    [Test]
    public async Task LogAccessAsync_WhenLogExists_ShouldIncrementCount()
    {
        // Arrange
        int documentId = 1;
        DateTime date = DateTime.Today;
        AccessLog existingLog = new AccessLog { Id = 1, Date = date, Count = 1 };
        List<AccessLog> logs = new List<AccessLog> { existingLog };

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
    public async Task AddLogToDocumentAsync_ShouldAddDocumentLog()
    {
        // Arrange
        int documentId = 1;
        string action = "OCR Completed";
        string details = "Text extracted successfully";

        _mockDocumentLogRepo.Setup(x => x.AddAsync(It.IsAny<DocumentLog>())).Returns(Task.CompletedTask);

        // Act
        await _documentService.AddLogToDocumentAsync(documentId, action, details);

        // Assert
        _mockDocumentLogRepo.Verify(x => x.AddAsync(It.Is<DocumentLog>(log =>
            log.Id == documentId && log.Action == action && log.Details == details)), Times.Once);
    }
}
