using API.Validators;
using BL.Messaging;
using BL.Services;
using Core.Configuration;
using Core.DTOs;
using Core.Messaging;
using Core.Repositories.Interfaces;
using DAL;
using DAL.Repositories.Implementations;
using FluentValidation;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces;
using DAL.Services;
using Elastic.Clients.Elasticsearch;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// --------------------
// Configure Logging
// --------------------
log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));

// --------------------
// Configure Database
// --------------------
builder.Services.AddDbContext<PaperlessDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// --------------------
// Register Repositories
// --------------------
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IAccessLogRepository, AccessLogRepository>();
builder.Services.AddScoped<IDocumentLogRepository, DocumentLogRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();

// --------------------
// Register Services (BL)
// --------------------
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<AccessLogService>();
builder.Services.AddScoped<ISearchService, SearchService>();

// ElasticSearch
builder.Services.AddSingleton<ElasticsearchClient>(sp =>
{
    var uri = builder.Configuration["ElasticSearch:Uri"] ?? "http://elasticsearch:9200";
    var settings = new ElasticsearchClientSettings(new Uri(uri))
        .DisableDirectStreaming(); // Capture Request/Response bytes
    return new ElasticsearchClient(settings);
});

// --------------------
// Register Messager
// --------------------
// --------------------
// Register Messager & Storage
// --------------------
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value);
builder.Services.AddSingleton<IDocumentMessageProducer, RabbitMqProducer>();

builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("Minio"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<MinioSettings>>().Value);
builder.Services.AddScoped<IStorageService, DAL.Services.MinioStorageService>();

// --------------------
// Register AutoMapper
// --------------------
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<DAL.Mappings.DalMappingProfile>();
    cfg.AddProfile<BL.Mappings.BlMappingProfile>();
});

// --------------------
// Register Validation
// --------------------
builder.Services.AddScoped<IValidator<DocumentDto>, DocumentDtoValidator>();
builder.Services.AddScoped<IValidator<AccessLogDto>, AccessLogDtoValidator>();
builder.Services.AddScoped<IValidator<DocumentLogDto>, DocumentLogDtoValidator>();
builder.Services.AddScoped<IValidator<TagDto>, TagDtoValidator>();

// --------------------
// Add Swagger
// --------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --------------------
// Add Controllers
// --------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --------------------
// Add CORS
// --------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); 
    options.ListenAnyIP(8081);
});

var app = builder.Build();

// --------------------
// Configure Middleware
// --------------------
// --------------------
// Configure Middleware
// --------------------
app.UseMiddleware<API.Middleware.ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();             
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty;
    });
}


app.UseHttpsRedirection();
app.UseCors();
//app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok("Healthy"));

// --------------------
// Run App
// --------------------
app.Run();