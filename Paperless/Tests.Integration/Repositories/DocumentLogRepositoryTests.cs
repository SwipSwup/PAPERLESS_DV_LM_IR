using AutoMapper;
using Core.Models;
using DAL;
using DAL.Mappings;
using DAL.Repositories.Implementations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.Integration.Repositories
{
    public class DocumentLogRepositoryTests
    {
        private readonly PaperlessDBContext _context;
        private readonly IMapper _mapper;
        private readonly DocumentLogRepository _repository;

        public DocumentLogRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<PaperlessDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PaperlessDBContext(options);
            
            var config = new MapperConfiguration(cfg => cfg.AddProfile<DalMappingProfile>());
            _mapper = config.CreateMapper();

            _repository = new DocumentLogRepository(_context, _mapper);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnLogs()
        {
             // Seed parent doc
            var doc = new DAL.Models.DocumentEntity { FileName = "Parent", FilePath = "p", UploadedAt = DateTime.UtcNow };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var log = new DAL.Models.DocumentLogEntity { Timestamp = DateTime.UtcNow, Action = "Test", DocumentId = doc.Id, DocumentEntity = doc };
            _context.DocumentLogs.Add(log);
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();
            result.Should().HaveCount(1);
            result.First().Action.Should().Be("Test");
        }

        [Fact]
        public async Task GetByDocumentIdAsync_ShouldReturnLogs()
        {
             var doc = new DAL.Models.DocumentEntity { FileName = "DocForLog2", FilePath = "p", UploadedAt = DateTime.UtcNow };
             _context.Documents.Add(doc);
             await _context.SaveChangesAsync();

             var log = new DAL.Models.DocumentLogEntity { Timestamp = DateTime.UtcNow, Action = "LogAction", DocumentId = doc.Id, DocumentEntity = doc };
             _context.DocumentLogs.Add(log);
             await _context.SaveChangesAsync();

             var result = await _repository.GetByDocumentIdAsync(doc.Id);
             result.Should().HaveCount(1);
             result.First().Action.Should().Be("LogAction");
        }
    }
}
