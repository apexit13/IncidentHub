using System.Security.Claims;
using IncidentHub.Api.Common;

namespace IncidentHub.Api.Middleware;
public class TestUserMiddleware
{
    private readonly RequestDelegate _next;

    public TestUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // X-Role header lets you test both roles in Scalar:
        //   X-Role: responder   → full access
        //   X-Role: viewer  → read only
        //   (omit)                 → defaults to responder for convenience
        var role = context.Request.Headers["X-Role"].ToString();
        if (string.IsNullOrEmpty(role))
            role = ClaimConstants.RoleTypeResponder;

        var claims = new[]
        {
            // Must match the namespace used in Program.cs authorization policies
            new Claim(ClaimConstants.RolesUri, role),
            new Claim(ClaimTypes.NameIdentifier, "dev-user")
        };

        var identity = new ClaimsIdentity(claims, "test-user");
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }
}
