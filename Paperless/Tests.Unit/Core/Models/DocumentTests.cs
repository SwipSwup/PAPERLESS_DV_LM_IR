using Core.Models;

namespace Tests.Unit.Core.Models;

[TestFixture]
public class DocumentTests
{
    private Document _document;

    [SetUp]
    public void Setup()
    {
        _document = new Document
        {
            Id = 1,
            FileName = "test.pdf",
            FilePath = "/path/to/test.pdf",
            UploadedAt = DateTime.UtcNow,
            Tags = new List<Tag>(),
            Logs = new List<DocumentLog>(),
            AccessLogs = new List<AccessLog>()
        };
    }

    [Test]
    public void LogAccess_WhenExistingLogExists_ShouldIncrementCount()
    {
        // Arrange
        var date = DateTime.Today;
        var existingLog = new AccessLog { Date = date, Count = 3 };
        _document.AccessLogs.Add(existingLog);

        // Act
        _document.LogAccess(date);

        // Assert
        Assert.That(_document.AccessLogs.Count, Is.EqualTo(1));
        Assert.That(existingLog.Count, Is.EqualTo(4));
    }

    [Test]
    public void LogAccess_WhenExistingLogOnDifferentDate_ShouldAddNewLog()
    {
        // Arrange
        var existingDate = DateTime.Today;
        var newDate = DateTime.Today.AddDays(1);
        var existingLog = new AccessLog { Date = existingDate, Count = 3 };
        _document.AccessLogs.Add(existingLog);

        // Act
        _document.LogAccess(newDate);

        // Assert
        Assert.That(_document.AccessLogs.Count, Is.EqualTo(2));
        var newLog = _document.AccessLogs.FirstOrDefault(l => l.Date.Date == newDate.Date);
        Assert.That(newLog, Is.Not.Null);
        Assert.That(newLog!.Count, Is.EqualTo(1));
        Assert.That(existingLog.Count, Is.EqualTo(3)); // Should remain unchanged
    }

    [Test]
    public void AddTag_WhenTagAlreadyExists_ShouldNotAddDuplicate()
    {
        // Arrange
        var tagName = "Important";
        _document.Tags.Add(new Tag { Name = tagName });

        // Act
        _document.AddTag(tagName);

        // Assert
        Assert.That(_document.Tags.Count, Is.EqualTo(1));
    }

    [Test]
    public void RemoveTag_WhenTagDoesNotExist_ShouldNotRemoveAnything()
    {
        // Arrange
        var tagName = "Important";
        var otherTag = new Tag { Name = "Archive" };
        _document.Tags.Add(otherTag);

        // Act
        _document.RemoveTag(tagName);

        // Assert
        Assert.That(_document.Tags.Count, Is.EqualTo(1));
        Assert.That(_document.Tags.First().Name, Is.EqualTo("Archive"));
    }

    [Test]
    public void AddLog_ShouldAddDocumentLog()
    {
        // Arrange
        var action = "OCR Completed";
        var details = "Text extracted successfully";

        // Act
        _document.AddLog(action, details);

        // Assert
        Assert.That(_document.Logs.Count, Is.EqualTo(1));
        var log = _document.Logs.First();
        Assert.That(log.Action, Is.EqualTo(action));
        Assert.That(log.Details, Is.EqualTo(details));
        Assert.That(log.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
    }

    [Test]
    public void AddLog_WithoutDetails_ShouldAddDocumentLogWithNullDetails()
    {
        // Arrange
        var action = "Document Uploaded";

        // Act
        _document.AddLog(action);

        // Assert
        Assert.That(_document.Logs.Count, Is.EqualTo(1));
        var log = _document.Logs.First();
        Assert.That(log.Action, Is.EqualTo(action));
        Assert.That(log.Details, Is.Null);
        Assert.That(log.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
    }

    [Test]
    public void AddLog_MultipleLogs_ShouldAddAllLogs()
    {
        // Arrange
        var action1 = "OCR Completed";
        var action2 = "Summary Generated";

        // Act
        _document.AddLog(action1);
        _document.AddLog(action2);

        // Assert
        Assert.That(_document.Logs.Count, Is.EqualTo(2));
        Assert.That(_document.Logs.Any(l => l.Action == action1), Is.True);
        Assert.That(_document.Logs.Any(l => l.Action == action2), Is.True);
    }
}
