using Serilog.Context;

namespace API.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task Invoke(HttpContext context)
    {
        string correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();

        // 1. Add to TraceIdentifier so ASP.NET native logs pick it up
        context.TraceIdentifier = correlationId;

        // 2. Push to Serilog Context so all logger calls have it
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Optional: return it to the caller
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
                return Task.CompletedTask;
            });

            await next(context);
        }
    }
}
