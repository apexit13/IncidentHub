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
        var user = httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirst("sub")?.Value ?? "anonymous";
        var userRole = user?.FindFirst(ClaimConstants.RolesUri)?.Value ?? "none";

        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("UserRole", userRole))
        {
            logger.LogInformation(
                "Handling {RequestName} by {UserId} [{UserRole}] {@Request}",
                requestName, userId, userRole, request);

            try
            {
                var response = await next();

                logger.LogInformation(
                    "Handled {RequestName} successfully by {UserId}",
                    requestName, userId);

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error handling {RequestName} by {UserId}",
                    requestName, userId);
                throw;
            }
        }
    }
}