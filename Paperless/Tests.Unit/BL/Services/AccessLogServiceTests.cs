using AutoMapper;
using BL.Services;
using Core.DTOs;
using Core.Models;
using Core.Repositories.Interfaces;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Unit.BL.Services
{
    [TestFixture]
    public class AccessLogServiceTests
    {
         private Mock<IAccessLogRepository> _mockRepo;
         private Mock<IMapper> _mockMapper;
         private AccessLogService _service;

         [SetUp]
         public void Setup()
         {
             _mockRepo = new Mock<IAccessLogRepository>();
             _mockMapper = new Mock<IMapper>();
             _service = new AccessLogService(_mockRepo.Object, _mockMapper.Object);
         }

         [Test]
         public async Task GetAllAsync_ShouldReturnMappedDtos()
         {
             var list = new List<AccessLog>();
             _mockRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(list);
             _mockMapper.Setup(x => x.Map<List<AccessLogDto>>(list)).Returns(new List<AccessLogDto>());

             var result = await _service.GetAllAsync();
             Assert.IsNotNull(result);
         }

         [Test]
         public async Task GetByIdAsync_ShouldReturnDto_WhenFound()
         {
             var entity = new AccessLog();
             _mockRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(entity);
             _mockMapper.Setup(x => x.Map<AccessLogDto>(entity)).Returns(new AccessLogDto());

             var result = await _service.GetByIdAsync(1);
             Assert.IsNotNull(result);
         }

         [Test]
         public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
         {
             _mockRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((AccessLog)null);
             var result = await _service.GetByIdAsync(1);
             Assert.IsNull(result);
         }

         [Test]
         public async Task AddAsync_ShouldCallRepo()
         {
             var entity = new AccessLog();
             _mockRepo.Setup(x => x.AddAsync(entity)).Returns(Task.CompletedTask);
             _mockMapper.Setup(x => x.Map<AccessLogDto>(entity)).Returns(new AccessLogDto());

             await _service.AddAsync(entity);
             _mockRepo.Verify(x => x.AddAsync(entity), Times.Once);
         }

         [Test]
         public async Task LogAccessAsync_ShouldIncrementCount_WhenExists()
         {
             var date = DateTime.Today;
             var existing = new AccessLog { Id = 1, Date = date, Count = 1 };
             _mockRepo.Setup(x => x.GetByDocumentIdAsync(1))
                 .ReturnsAsync(new List<AccessLog> { existing });

             await _service.LogAccessAsync(1, date);

             Assert.That(existing.Count, Is.EqualTo(2));
             _mockRepo.Verify(x => x.UpdateAsync(existing), Times.Once);
         }

         [Test]
         public async Task LogAccessAsync_ShouldCreateNew_WhenNotExists()
         {
             var date = DateTime.Today;
             _mockRepo.Setup(x => x.GetByDocumentIdAsync(1))
                 .ReturnsAsync(new List<AccessLog>());

             await _service.LogAccessAsync(1, date);

             _mockRepo.Verify(x => x.AddAsync(It.Is<AccessLog>(a => a.Count == 1 && a.Id == 1)), Times.Once);
         }
         
         // Adding negative tests
         [Test]
         public async Task DeleteAsync_ShouldReturnFalse_WhenNotFound()
         {
             _mockRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((AccessLog)null);
             var result = await _service.DeleteAsync(1);
             Assert.IsFalse(result);
         }
    }
}
