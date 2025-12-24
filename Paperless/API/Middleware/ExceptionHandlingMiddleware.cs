using System.Net;
using System.Text.Json;
using Core.Exceptions;
using FluentValidation;
using log4net;
using System.Reflection;

namespace API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                error = exception.Message,
                type = exception.GetType().Name
            };

            switch (exception)
            {
                case ValidationException ex:
                    log.Warn($"Validation failed: {ex.Message}");
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case ServiceException ex:
                    log.Error($"Service error: {ex.Message}", ex);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
                case DataAccessException ex:
                    log.Error($"Data access error: {ex.Message}", ex);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
                case MessagingException ex:
                    log.Error($"Messaging error: {ex.Message}", ex);
                    context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    break;
                default:
                    log.Error($"Unhandled exception: {exception.Message}", exception);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
