using BL.Messaging;
using BL.Services;
using Core.Messaging;
using Core.Repositories.Interfaces;
using DAL;
using DAL.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

// --------------------
// Register Messager
// --------------------
builder.Services.AddScoped<IDocumentMessageProducer, RabbitMqProducer>();

// --------------------
// Register AutoMapper
// --------------------
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<DAL.Mappings.DalMappingProfile>();
    cfg.AddProfile<BL.Mappings.BlMappingProfile>();
});

// --------------------
// Add Controllers
// --------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

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
/*if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}*/

app.UseHttpsRedirection();
app.UseCors();
//app.UseAuthorization();
app.MapControllers();

// --------------------
// Run App
// --------------------
app.Run();