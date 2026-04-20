using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace IncidentHub.Api.Infrastructure.Services;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? UserEmail { get; }
    bool IsAuthenticated { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
                              ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("name");

    public string? UserEmail => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
                              ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}