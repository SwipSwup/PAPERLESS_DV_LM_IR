using BL.Services;
using Core.Models;
using Core.Repositories.Interfaces;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Unit.BL.Services
{
    [TestFixture]
    public class TagServiceTests
    {
        private Mock<ITagRepository> _mockRepo;
        private TagService _service;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<ITagRepository>();
            _service = new TagService(_mockRepo.Object);
        }

        [Test]
        public async Task GetAllTagsAsync_ShouldCallRepo()
        {
            _mockRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Tag>());
            List<Tag> result = await _service.GetAllTagsAsync();
            Assert.IsNotNull(result);
            _mockRepo.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Test]
        public async Task GetTagByIdAsync_ShouldReturnTag()
        {
            Tag tag = new Tag { Id = 1, Name = "Test" };
            _mockRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(tag);
            Tag? result = await _service.GetTagByIdAsync(1);
            Assert.That(result, Is.EqualTo(tag));
        }

        [Test]
        public async Task AddTagAsync_ShouldCallRepo()
        {
            Tag tag = new Tag();
            _mockRepo.Setup(x => x.AddAsync(tag)).Returns(Task.CompletedTask);
            await _service.AddTagAsync(tag);
            _mockRepo.Verify(x => x.AddAsync(tag), Times.Once);
        }

        [Test]
        public async Task UpdateTagAsync_ShouldCallRepo()
        {
            Tag tag = new Tag { Id = 1 };
            _mockRepo.Setup(x => x.UpdateAsync(tag)).Returns(Task.CompletedTask);
            await _service.UpdateTagAsync(tag);
            _mockRepo.Verify(x => x.UpdateAsync(tag), Times.Once);
        }

        [Test]
        public async Task DeleteTagAsync_ShouldCallRepo()
        {
            await _service.DeleteTagAsync(1);
            _mockRepo.Verify(x => x.DeleteAsync(1), Times.Once);
        }

        [Test]
        public async Task SearchTagsAsync_ShouldCallRepo()
        {
            _mockRepo.Setup(x => x.SearchTagsAsync("key")).ReturnsAsync(new List<Tag>());
            await _service.SearchTagsAsync("key");
            _mockRepo.Verify(x => x.SearchTagsAsync("key"), Times.Once);
        }
    }
}
