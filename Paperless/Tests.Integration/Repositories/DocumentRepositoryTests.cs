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
    public class DocumentRepositoryTests
    {
        private readonly PaperlessDBContext _context;
        private readonly IMapper _mapper;
        private readonly DocumentRepository _repository;

        public DocumentRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<PaperlessDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PaperlessDBContext(options);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<DalMappingProfile>());
            _mapper = config.CreateMapper();

            _repository = new DocumentRepository(_context, _mapper);
        }

        [Fact]
        public async Task AddAsync_ShouldAddDocument()
        {
            var doc = new Document { FileName = "Test Doc", UploadedAt = DateTime.UtcNow };
            await _repository.AddAsync(doc);

            var inDb = await _context.Documents.FirstOrDefaultAsync();
            inDb.Should().NotBeNull();
            inDb!.FileName.Should().Be("Test Doc");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDocument()
        {
            var entity = new DAL.Models.DocumentEntity { FileName = "Existing", FilePath = "path", UploadedAt = DateTime.UtcNow };
            _context.Documents.Add(entity);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(entity.Id);
            result.Should().NotBeNull();
            result!.FileName.Should().Be("Existing");
        }
    }
}
