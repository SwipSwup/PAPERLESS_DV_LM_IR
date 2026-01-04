using Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails = new ProblemDetails { Instance = httpContext.Request.Path };

        switch (exception)
        {
            case DomainException domainEx:
                problemDetails.Title = "Bad Request";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = domainEx.Message;
            
                // Log as warning - client side error
                logger.LogWarning(exception, "Domain exception occurred: {Message}", domainEx.Message);
                break;
            case FluentValidation.ValidationException validationEx:
                problemDetails.Title = "Validation Failed";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = "One or more validation errors occurred.";
                problemDetails.Extensions["errors"] = validationEx.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage });
            
                logger.LogWarning(exception, "Validation exception occurred");
                break;
            case InfrastructureException infraEx:
                problemDetails.Title = "Service Unavailable";
                problemDetails.Status = StatusCodes.Status503ServiceUnavailable;
                problemDetails.Detail = "A service error occurred.";
            
                // Log as error - system error
                logger.LogError(exception, "Infrastructure exception: {Message}", infraEx.Message);
                break;
            default:
                // Mask internal errors
                logger.LogError(exception, "Unhandled system exception");
                problemDetails.Title = "Server Error";
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Detail = "An internal error occurred.";
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
