using AutoMapper;
using Core.Models;
using DAL;
using DAL.Mappings;
using DAL.Models;
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
            DbContextOptions<PaperlessDBContext> options = new DbContextOptionsBuilder<PaperlessDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PaperlessDBContext(options);

            MapperConfiguration config = new MapperConfiguration(cfg => cfg.AddProfile<DalMappingProfile>());
            _mapper = config.CreateMapper();

            _repository = new DocumentRepository(_context, _mapper);
        }

        [Fact]
        public async Task AddAsync_ShouldAddDocument()
        {
            Document doc = new Document { FileName = "Test Doc", UploadedAt = DateTime.UtcNow };
            await _repository.AddAsync(doc);

            DocumentEntity? inDb = await _context.Documents.FirstOrDefaultAsync();
            inDb.Should().NotBeNull();
            inDb!.FileName.Should().Be("Test Doc");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDocument()
        {
            DocumentEntity entity = new DAL.Models.DocumentEntity { FileName = "Existing", FilePath = "path", UploadedAt = DateTime.UtcNow };
            _context.Documents.Add(entity);
            await _context.SaveChangesAsync();

            Document? result = await _repository.GetByIdAsync(entity.Id);
            result.Should().NotBeNull();
            result!.FileName.Should().Be("Existing");
        }
    }
}
