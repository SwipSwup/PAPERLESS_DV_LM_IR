using BatchWorker;
using DAL;
using Microsoft.EntityFrameworkCore;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PaperlessDBContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();