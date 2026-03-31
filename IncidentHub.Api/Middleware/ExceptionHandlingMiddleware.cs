using System.Net;
using System.Text.Json;

namespace IncidentHub.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            KeyNotFoundException    => (HttpStatusCode.NotFound,            ex.Message),
            InvalidOperationException => (HttpStatusCode.Conflict,          ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden,       ex.Message),
            ArgumentException       => (HttpStatusCode.BadRequest,          ex.Message),
            _                       => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        var body = JsonSerializer.Serialize(new
        {
            status  = (int)statusCode,
            error   = message
        });

        return context.Response.WriteAsync(body);
    }
}
