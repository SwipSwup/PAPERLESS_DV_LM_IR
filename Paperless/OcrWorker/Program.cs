using OcrWorker;
using OcrWorker.Config;
using Core.Configuration;
using OcrWorker.Messaging;
using OcrWorker.Services.Pdf;
using OcrWorker.Services.Ocr;
using OcrWorker.Services.Tesseract;
using PaperlessServices.OcrWorker.Ocr;
using OcrWorker.Storage;
using OcrWorker.Utils;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Config
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("Minio"));
builder.Services.Configure<OcrSettings>(builder.Configuration.GetSection("Ocr"));

builder.Services.AddScoped<IMessageConsumer, MessageConsumer>();
builder.Services.AddScoped<IPdfConverter, DynamicPdfConverter>();
builder.Services.AddScoped<IStorageWrapper, StorageWrapper>();
builder.Services.AddScoped<ITesseractCliRunner, TesseractCliRunner>();
builder.Services.AddScoped<IOcrService, OcrService>();

builder.Services.AddScoped<ITempFileUtility, TempFileUtility>();

builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();