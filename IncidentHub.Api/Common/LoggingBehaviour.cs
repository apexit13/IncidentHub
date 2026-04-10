using MediatR;
using Serilog.Context;

namespace IncidentHub.Api.Common;

public class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
    IHttpContextAccessor httpContextAccessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;

        // Only log important operations to reduce noise
        if (IsImportantOperation(requestName))
        {
            var user = httpContextAccessor.HttpContext?.User;
            var userId = user?.FindFirst("sub")?.Value ?? "anonymous";
            var userRole = user?.FindFirst(ClaimConstants.RolesUri)?.Value ?? "none";

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("UserRole", userRole))
            using (LogContext.PushProperty("RequestName", requestName))
            {
                logger.LogInformation(
                    "Processing {RequestName} for user {UserId} [{UserRole}]",
                    requestName, userId, userRole);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var response = await next();

                    logger.LogInformation(
                        "Completed {RequestName} in {ElapsedMs}ms for user {UserId}",
                        requestName, stopwatch.ElapsedMilliseconds, userId);

                    return response;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed {RequestName} after {ElapsedMs}ms for user {UserId}",
                        requestName, stopwatch.ElapsedMilliseconds, userId);
                    throw;
                }
            }
        }
        else
        {
            // Skip logging for less important operations
            return await next();
        }
    }

    private static bool IsImportantOperation(string requestName)
    {
        // Only log incident-related commands, not queries or minor operations
        return requestName.Contains("Incident") &&
               (requestName.Contains("Create") ||
                requestName.Contains("Update") ||
                requestName.Contains("Resolve") ||
                requestName.Contains("Raise"));
    }
}