using IndexingWorker;
using Core.Configuration;
using IndexingWorker.Messaging;
using DAL;
using DAL.Repositories.Implementations;
using Core.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Elastic.Clients.Elasticsearch;

var builder = Host.CreateApplicationBuilder(args);

// Config
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

// Database
builder.Services.AddDbContext<PaperlessDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Repositories
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<DAL.Mappings.DalMappingProfile>();
});
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// Messaging
builder.Services.AddSingleton<IMessageConsumer, MessageConsumer>();

// ElasticSearch
builder.Services.AddSingleton<ElasticsearchClient>(sp =>
{
    var uri = builder.Configuration["ElasticSearch:Uri"] ?? "http://elasticsearch:9200";
    return new ElasticsearchClient(new Uri(uri));
});

// Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();