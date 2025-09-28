using AutoMapper;
using BL.Services;
using Core.DTOs;
using Core.Models;
using Core.Repositories.Interfaces;
using Moq;

namespace Tests.Unit.BL.Services;

[TestFixture]
public class AccessLogServiceTests
{
    private Mock<IAccessLogRepository> _mockAccessLogRepo;
    private Mock<IMapper> _mockMapper;
    private AccessLogService _accessLogService;

    [SetUp]
    public void Setup()
    {
        _mockAccessLogRepo = new Mock<IAccessLogRepository>();
        _mockMapper = new Mock<IMapper>();
        _accessLogService = new AccessLogService(_mockAccessLogRepo.Object, _mockMapper.Object);
    }

    [Test]
    public async Task GetByIdAsync_WhenAccessLogExists_ShouldReturnMappedAccessLog()
    {
        // Arrange
        var accessLog = new AccessLog { Id = 1, Date = DateTime.Today, Count = 5 };
        var expectedDto = new AccessLogDto { Date = DateTime.Today, Count = 5 };

        _mockAccessLogRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(accessLog);
        _mockMapper.Setup(x => x.Map<AccessLogDto>(accessLog)).Returns(expectedDto);

        // Act
        var result = await _accessLogService.GetByIdAsync(1);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDto));
        _mockAccessLogRepo.Verify(x => x.GetByIdAsync(1), Times.Once);
    }

    [Test]
    public async Task GetByDocumentIdAsync_ShouldReturnMappedAccessLogs()
    {
        // Arrange
        var documentId = 1;
        var accessLogs = new List<AccessLog>
        {
            new() { Id = 1, Date = DateTime.Today, Count = 5 }
        };
        var expectedDtos = new List<AccessLogDto>
        {
            new() { Date = DateTime.Today, Count = 5 }
        };

        _mockAccessLogRepo.Setup(x => x.GetByDocumentIdAsync(documentId)).ReturnsAsync(accessLogs);
        _mockMapper.Setup(x => x.Map<List<AccessLogDto>>(accessLogs)).Returns(expectedDtos);

        // Act
        var result = await _accessLogService.GetByDocumentIdAsync(documentId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDtos));
        _mockAccessLogRepo.Verify(x => x.GetByDocumentIdAsync(documentId), Times.Once);
        _mockMapper.Verify(x => x.Map<List<AccessLogDto>>(accessLogs), Times.Once);
    }

    [Test]
    public async Task AddAsync_ShouldAddAccessLogAndReturnMappedDto()
    {
        // Arrange
        var accessLog = new AccessLog { Date = DateTime.Today, Count = 1 };
        var expectedDto = new AccessLogDto { Date = DateTime.Today, Count = 1 };

        _mockAccessLogRepo.Setup(x => x.AddAsync(accessLog)).Returns(Task.CompletedTask);
        _mockMapper.Setup(x => x.Map<AccessLogDto>(accessLog)).Returns(expectedDto);

        // Act
        var result = await _accessLogService.AddAsync(accessLog);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDto));
        _mockAccessLogRepo.Verify(x => x.AddAsync(accessLog), Times.Once);
        _mockMapper.Verify(x => x.Map<AccessLogDto>(accessLog), Times.Once);
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
        await _accessLogService.LogAccessAsync(documentId, date);

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
        await _accessLogService.LogAccessAsync(documentId, date);

        // Assert
        _mockAccessLogRepo.Verify(x => x.GetByDocumentIdAsync(documentId), Times.Once);
        _mockAccessLogRepo.Verify(x => x.AddAsync(It.Is<AccessLog>(log => 
            log.Id == documentId && log.Date == date && log.Count == 1)), Times.Once);
    }
}
