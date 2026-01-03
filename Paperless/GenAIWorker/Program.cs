using GenAIWorker;
using BL.Messaging;
using Core.Configuration;
using GenAIWorker.Messaging;
using GenAIWorker.Services;
using DAL;
using DAL.Repositories.Implementations;
using Core.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Core.Messaging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

    // Remove default logging providers
    builder.Logging.ClearProviders();
    builder.Services.AddSerilog();

// Config
    builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
    builder.Services.Configure<GenAISettings>(builder.Configuration.GetSection("GenAI"));

// Database
    builder.Services.AddDbContext<PaperlessDBContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    );

// Repositories - AutoMapper configured via extension method
    builder.Services.AddAutoMapper(cfg => { cfg.AddProfile<DAL.Mappings.DalMappingProfile>(); });

// Repositories
    builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// Messaging
    builder.Services.AddSingleton<IMessageConsumer, MessageConsumer>();

    builder.Services.AddSingleton<IDocumentMessageProducer>(sp =>
    {
        RabbitMqSettings settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
        RabbitMqSettings producerSettings = new RabbitMqSettings
        {
            Host = settings.Host,
            Port = settings.Port,
            User = settings.User,
            Password = settings.Password,
            QueueName = "indexing"
        };
        var logger = sp.GetRequiredService<ILogger<RabbitMqProducer>>();
        return new RabbitMqProducer(producerSettings, logger);
    });

// GenAI Service
    builder.Services.AddHttpClient<GenAiService>(client => { client.Timeout = TimeSpan.FromSeconds(60); });
    builder.Services.AddScoped<IGenAIService, GenAiService>();

// Worker
    builder.Services.AddHostedService<Worker>();

    IHost host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}