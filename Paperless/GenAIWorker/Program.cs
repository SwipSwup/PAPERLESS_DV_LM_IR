using GenAIWorker;
using Core.Configuration;
using GenAIWorker.Messaging;
using GenAIWorker.Services;
using DAL;
using DAL.Repositories.Implementations;
using Core.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

var builder = Host.CreateApplicationBuilder(args);

// Config
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<GenAISettings>(builder.Configuration.GetSection("GenAI"));

// Database
builder.Services.AddDbContext<PaperlessDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Repositories - AutoMapper configured via extension method
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<DAL.Mappings.DalMappingProfile>();
});

// Repositories
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// Messaging
builder.Services.AddSingleton<IMessageConsumer, MessageConsumer>();

// GenAI Service
builder.Services.AddHttpClient<GenAIService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddScoped<IGenAIService, GenAIService>();

// Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
