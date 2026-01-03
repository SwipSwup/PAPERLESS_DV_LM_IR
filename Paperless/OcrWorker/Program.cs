using OcrWorker;
using OcrWorker.Config;
using Core.Configuration;
using OcrWorker.Messaging;
using OcrWorker.Services.Pdf;
using OcrWorker.Services.Ocr;
using OcrWorker.Services.Tesseract;
using OcrWorker.Storage;
using OcrWorker.Utils;
using DAL;
using DAL.Repositories.Implementations;
using Core.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using BL.Messaging;
using Core.Messaging;
using Core.DTOs;
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
    builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("Minio"));
    builder.Services.Configure<OcrSettings>(builder.Configuration.GetSection("Ocr"));

// Database
    builder.Services.AddDbContext<PaperlessDBContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    );

// Repositories
    builder.Services.AddAutoMapper(cfg => { cfg.AddProfile<DAL.Mappings.DalMappingProfile>(); });

    builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

    builder.Services.AddSingleton<IMessageConsumer, MessageConsumer>();
    builder.Services
        .AddSingleton<OcrWorker.Messaging.IDocumentMessageProducerFactory,
            OcrWorker.Messaging.DocumentMessageProducerFactory>();
    builder.Services.AddScoped<IPdfConverter, DynamicPdfConverter>();
    builder.Services.AddScoped<IStorageWrapper, StorageWrapper>();
    builder.Services.AddScoped<ITesseractCliRunner, TesseractCliRunner>();
    builder.Services.AddScoped<IOcrService, OcrService>();

    builder.Services.AddScoped<ITempFileUtility, TempFileUtility>();

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