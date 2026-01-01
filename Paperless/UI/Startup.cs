using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UI
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddHttpClient();
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            // app.UseHttpsRedirection(); // Disable HTTPS redirection to avoid cert issues in dev proxy
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            // Simple API Proxy Middleware
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                
                endpoints.Map("/api/{**catch-all}", async context =>
                {
                    var httpClientFactory = context.RequestServices.GetRequiredService<System.Net.Http.IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient();
                    
                    var targetUri = new Uri("http://paperless-api:8080" + context.Request.Path + context.Request.QueryString);
                    Console.WriteLine($"[Proxy] Forwarding to: {targetUri}");
                    
                    var requestMessage = new System.Net.Http.HttpRequestMessage();
                    requestMessage.RequestUri = targetUri;
                    requestMessage.Method = new System.Net.Http.HttpMethod(context.Request.Method);
                    
                    if (context.Request.Body != null)
                    {
                         var streamContent = new System.Net.Http.StreamContent(context.Request.Body);
                         requestMessage.Content = streamContent;
                    }

                    // Copy headers
                    foreach (var header in context.Request.Headers)
                    {
                        if (!header.Key.StartsWith("Host", System.StringComparison.OrdinalIgnoreCase) &&
                            !header.Key.StartsWith("Connection", System.StringComparison.OrdinalIgnoreCase))
                        {
                            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                        }
                    }

                    var responseMessage = await httpClient.SendAsync(requestMessage);
                    Console.WriteLine($"[Proxy] API Response: {responseMessage.StatusCode}");
                    
                    context.Response.StatusCode = (int)responseMessage.StatusCode;
                    
                    // Copy response headers (excluding hop-by-hop)
                    foreach (var header in responseMessage.Headers)
                    {
                        if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) &&
                            !header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.Headers[header.Key] = header.Value.ToArray();
                        }
                    }
                    foreach (var header in responseMessage.Content.Headers)
                    {
                          if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) // Let Kestrel calculate length
                          {
                               context.Response.Headers[header.Key] = header.Value.ToArray();
                          }
                    }
                    
                    await responseMessage.Content.CopyToAsync(context.Response.Body);
                });
            });
        }
    }
}