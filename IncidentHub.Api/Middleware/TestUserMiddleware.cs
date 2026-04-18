using System.Security.Claims;
using IncidentHub.Api.Constants;
using static IncidentHub.Api.Constants.AuthPolicies;

namespace IncidentHub.Api.Middleware;
public class TestUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;

    public TestUserMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get permission set from header or use default
        var permissionSet = context.Request.Headers["X-Permissions"].ToString();
        if (string.IsNullOrEmpty(permissionSet))
            permissionSet = _config["TestUser:DefaultPermissionSet"] ?? "admin";

        var permissions = GetPermissionsForSet(permissionSet);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "dev-user")
        };

        // Add each permission as an individual claim
        foreach (var permission in permissions)
        {
            claims.Add(new Claim(AuthClaimTypes.Permissions, permission));
        }
        var identity = new ClaimsIdentity(claims, "TestUser");
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }
    private static string[] GetPermissionsForSet(string permissionSet)
    {
        return permissionSet.ToLowerInvariant() switch
        {
            "admin" =>
            [
                Permissions.ManageIncidents,
                Permissions.ReadIncidents,
                Permissions.ReadUsers
            ],
            "responder" =>
            [
                Permissions.ManageIncidents,
                Permissions.ReadIncidents
            ],
            "viewer" =>
            [
                Permissions.ReadIncidents
            ],
            _ => []
        };
    }
}
