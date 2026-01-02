using AutoMapper;
using Core.Models;
using DAL;
using DAL.Mappings;
using DAL.Repositories.Implementations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Integration.Repositories
{
    public class AccessLogRepositoryTests
    {
        private readonly PaperlessDBContext _context;
        private readonly IMapper _mapper;
        private readonly AccessLogRepository _repository;

        public AccessLogRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<PaperlessDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test class
                .Options;

            _context = new PaperlessDBContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<DalMappingProfile>());
            _mapper = config.CreateMapper();

            _repository = new AccessLogRepository(_context, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnLogs()
        {
            // Seed DB with an Entity directly to ensure validity
            var doc = new DAL.Models.DocumentEntity { FileName = "Test", FilePath = "path", UploadedAt = DateTime.UtcNow };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var log = new DAL.Models.AccessLogEntity { Date = DateTime.UtcNow, Count = 1, DocumentId = doc.Id, DocumentEntity = doc };
            _context.AccessLogs.Add(log);
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByDocumentIdAsync_ShouldReturnLogs()
        {
            var doc = new DAL.Models.DocumentEntity { FileName = "DocForLog", FilePath = "p", UploadedAt = DateTime.UtcNow };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var log = new DAL.Models.AccessLogEntity { Date = DateTime.UtcNow, Count = 5, DocumentId = doc.Id, DocumentEntity = doc };
            _context.AccessLogs.Add(log);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByDocumentIdAsync(doc.Id);
            result.Should().HaveCount(1);
            result.First().Count.Should().Be(5);
        }
    }
}
