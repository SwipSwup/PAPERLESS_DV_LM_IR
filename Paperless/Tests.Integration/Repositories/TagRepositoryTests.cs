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
    public class TagRepositoryTests
    {
        private readonly PaperlessDBContext _context;
        private readonly IMapper _mapper;
        private readonly TagRepository _repository;

        public TagRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<PaperlessDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PaperlessDBContext(options);
            
            var config = new MapperConfiguration(cfg => cfg.AddProfile<DalMappingProfile>());
            _mapper = config.CreateMapper();

            _repository = new TagRepository(_context, _mapper);
        }

        [Fact]
        public async Task AddAsync_ShouldAddTag()
        {
            var tag = new Tag { Name = "New Tag" };
            await _repository.AddAsync(tag);

            var inDb = await _context.Tags.FirstOrDefaultAsync();
            inDb.Should().NotBeNull();
            inDb!.Name.Should().Be("New Tag");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnTag()
        {
            var entity = new DAL.Models.TagEntity { Name = "Existing Tag" };
            _context.Tags.Add(entity);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(entity.Id);
            result.Should().NotBeNull();
            result!.Name.Should().Be("Existing Tag");
        }

         [Fact]
        public async Task SearchTagsAsync_ShouldReturnMatches()
        {
            _context.Tags.Add(new DAL.Models.TagEntity { Name = "Invoice 2023" });
            _context.Tags.Add(new DAL.Models.TagEntity { Name = "Receipt" });
            await _context.SaveChangesAsync();

            var result = await _repository.SearchTagsAsync("Invoice");
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Invoice 2023");
        }
    }
}
