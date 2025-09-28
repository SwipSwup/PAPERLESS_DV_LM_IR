using BL.Services;
using Core.Models;
using Core.Repositories.Interfaces;
using Moq;

namespace Tests.Unit.BL.Services;

[TestFixture]
public class TagServiceTests
{
    private Mock<ITagRepository> _mockTagRepository;
    private TagService _tagService;

    [SetUp]
    public void Setup()
    {
        _mockTagRepository = new Mock<ITagRepository>();
        _tagService = new TagService(_mockTagRepository.Object);
    }

    [Test]
    public async Task GetAllTagsAsync_ShouldReturnAllTags()
    {
        // Arrange
        var expectedTags = new List<Tag>
        {
            new() { Id = 1, Name = "Important" },
            new() { Id = 2, Name = "Archive" }
        };

        _mockTagRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedTags);

        // Act
        var result = await _tagService.GetAllTagsAsync();

        // Assert
        Assert.That(result, Is.EqualTo(expectedTags));
        _mockTagRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetTagByIdAsync_WhenTagExists_ShouldReturnTag()
    {
        // Arrange
        var expectedTag = new Tag { Id = 1, Name = "Important" };
        _mockTagRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(expectedTag);

        // Act
        var result = await _tagService.GetTagByIdAsync(1);

        // Assert
        Assert.That(result, Is.EqualTo(expectedTag));
        _mockTagRepository.Verify(x => x.GetByIdAsync(1), Times.Once);
    }

    [Test]
    public async Task GetTagByIdAsync_WhenTagDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _mockTagRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Tag?)null);

        // Act
        var result = await _tagService.GetTagByIdAsync(1);

        // Assert
        Assert.That(result, Is.Null);
        _mockTagRepository.Verify(x => x.GetByIdAsync(1), Times.Once);
    }

    [Test]
    public async Task AddTagAsync_ShouldAddTagAndReturnIt()
    {
        // Arrange
        var tag = new Tag { Name = "Important" };
        _mockTagRepository.Setup(x => x.AddAsync(tag)).Returns(Task.CompletedTask);

        // Act
        var result = await _tagService.AddTagAsync(tag);

        // Assert
        Assert.That(result, Is.EqualTo(tag));
        _mockTagRepository.Verify(x => x.AddAsync(tag), Times.Once);
    }

    [Test]
    public async Task UpdateTagAsync_ShouldUpdateTagAndReturnIt()
    {
        // Arrange
        var tag = new Tag { Id = 1, Name = "Important Updated" };
        _mockTagRepository.Setup(x => x.UpdateAsync(tag)).Returns(Task.CompletedTask);

        // Act
        var result = await _tagService.UpdateTagAsync(tag);

        // Assert
        Assert.That(result, Is.EqualTo(tag));
        _mockTagRepository.Verify(x => x.UpdateAsync(tag), Times.Once);
    }

    [Test]
    public async Task DeleteTagAsync_ShouldDeleteTag()
    {
        // Arrange
        var tagId = 1;
        _mockTagRepository.Setup(x => x.DeleteAsync(tagId)).Returns(Task.CompletedTask);

        // Act
        await _tagService.DeleteTagAsync(tagId);

        // Assert
        _mockTagRepository.Verify(x => x.DeleteAsync(tagId), Times.Once);
    }

    [Test]
    public async Task SearchTagsAsync_ShouldReturnMatchingTags()
    {
        // Arrange
        var keyword = "Important";
        var expectedTags = new List<Tag>
        {
            new() { Id = 1, Name = "Important" }
        };

        _mockTagRepository.Setup(x => x.SearchTagsAsync(keyword)).ReturnsAsync(expectedTags);

        // Act
        var result = await _tagService.SearchTagsAsync(keyword);

        // Assert
        Assert.That(result, Is.EqualTo(expectedTags));
        _mockTagRepository.Verify(x => x.SearchTagsAsync(keyword), Times.Once);
    }

    [Test]
    public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TagService(null!));
    }
}
