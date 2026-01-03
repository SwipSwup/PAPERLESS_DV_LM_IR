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
        DateTime date = DateTime.Today;
        AccessLog existingLog = new AccessLog { Date = date, Count = 3 };
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
        DateTime existingDate = DateTime.Today;
        DateTime newDate = DateTime.Today.AddDays(1);
        AccessLog existingLog = new AccessLog { Date = existingDate, Count = 3 };
        _document.AccessLogs.Add(existingLog);

        // Act
        _document.LogAccess(newDate);

        // Assert
        Assert.That(_document.AccessLogs.Count, Is.EqualTo(2));
        AccessLog? newLog = _document.AccessLogs.FirstOrDefault(l => l.Date.Date == newDate.Date);
        Assert.That(newLog, Is.Not.Null);
        Assert.That(newLog!.Count, Is.EqualTo(1));
        Assert.That(existingLog.Count, Is.EqualTo(3)); // Should remain unchanged
    }

    [Test]
    public void AddTag_WhenTagAlreadyExists_ShouldNotAddDuplicate()
    {
        // Arrange
        string tagName = "Important";
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
        string tagName = "Important";
        Tag otherTag = new Tag { Name = "Archive" };
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
        string action = "OCR Completed";
        string details = "Text extracted successfully";

        // Act
        _document.AddLog(action, details);

        // Assert
        Assert.That(_document.Logs.Count, Is.EqualTo(1));
        DocumentLog log = _document.Logs.First();
        Assert.That(log.Action, Is.EqualTo(action));
        Assert.That(log.Details, Is.EqualTo(details));
        Assert.That(log.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
    }

    [Test]
    public void AddLog_WithoutDetails_ShouldAddDocumentLogWithNullDetails()
    {
        // Arrange
        string action = "Document Uploaded";

        // Act
        _document.AddLog(action);

        // Assert
        Assert.That(_document.Logs.Count, Is.EqualTo(1));
        DocumentLog log = _document.Logs.First();
        Assert.That(log.Action, Is.EqualTo(action));
        Assert.That(log.Details, Is.Null);
        Assert.That(log.Timestamp, Is.Not.EqualTo(DateTime.MinValue));
    }

    [Test]
    public void AddLog_MultipleLogs_ShouldAddAllLogs()
    {
        // Arrange
        string action1 = "OCR Completed";
        string action2 = "Summary Generated";

        // Act
        _document.AddLog(action1);
        _document.AddLog(action2);

        // Assert
        Assert.That(_document.Logs.Count, Is.EqualTo(2));
        Assert.That(_document.Logs.Any(l => l.Action == action1), Is.True);
        Assert.That(_document.Logs.Any(l => l.Action == action2), Is.True);
    }
}
