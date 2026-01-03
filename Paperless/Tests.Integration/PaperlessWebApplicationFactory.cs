using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DAL;
using System.Linq;
using Core.Interfaces;
using Core.Messaging;
using Moq;

namespace Tests.Integration
{
    public class PaperlessWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                ServiceDescriptor? descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<PaperlessDBContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<PaperlessDBContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // Remove existing IStorageService
                ServiceDescriptor? storageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Core.Interfaces.IStorageService));
                if (storageDescriptor != null) services.Remove(storageDescriptor);

                // Remove existing IDocumentMessageProducer
                ServiceDescriptor? producerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Core.Messaging.IDocumentMessageProducer));
                if (producerDescriptor != null) services.Remove(producerDescriptor);

                // Add Mocks
                services.AddScoped<Core.Interfaces.IStorageService>(sp =>
                {
                    Mock<IStorageService> mock = new Moq.Mock<Core.Interfaces.IStorageService>();
                    mock.Setup(x => x.UploadFileAsync(Moq.It.IsAny<System.IO.Stream>(), Moq.It.IsAny<string>(), Moq.It.IsAny<string>()))
                        .Returns(Task.FromResult("mock/path/file.pdf"));
                    return mock.Object;
                });

                services.AddScoped<Core.Messaging.IDocumentMessageProducer>(sp =>
                {
                    Mock<IDocumentMessageProducer> mock = new Moq.Mock<Core.Messaging.IDocumentMessageProducer>();
                    return mock.Object;
                });

                // Remove ISearchService
                ServiceDescriptor? searchDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Core.Interfaces.ISearchService));
                if (searchDescriptor != null) services.Remove(searchDescriptor);

                services.AddScoped<Core.Interfaces.ISearchService>(sp =>
                {
                    Mock<ISearchService> mock = new Moq.Mock<Core.Interfaces.ISearchService>();
                    mock.Setup(x => x.SearchDocumentsAsync(Moq.It.IsAny<string>()))
                        .Returns(Task.FromResult<IEnumerable<Core.DTOs.DocumentDto>>(new List<Core.DTOs.DocumentDto>()));
                    return mock.Object;
                });
            });
        }
    }
}
