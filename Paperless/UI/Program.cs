using UI.Components;
using UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HTTP client services
builder.Services.AddHttpClient<IDocumentService, DocumentService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7001/"); // API base URL
});

builder.Services.AddHttpClient<ITagService, TagService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7001/"); // API base URL
});

builder.Services.AddHttpClient<IAccessLogService, AccessLogService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7001/"); // API base URL
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();