using System.Security.Claims;
using System.Text.Encodings.Web;
using IncidentHub.Api.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using static IncidentHub.Api.Constants.AuthPolicies;

namespace IncidentHub.Tests.Infrastructure.Auth;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var permissionSet = Request.Headers["X-Permissions"].ToString();

        if (string.IsNullOrEmpty(permissionSet))
            return Task.FromResult(AuthenticateResult.Fail("No X-Permissions header"));

        var permissions = GetPermissionsForSet(permissionSet);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "auth0|test-user-id"),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, "test@example.com"),
            new("sub", "auth0|test-user-id"),
        };

        foreach (var permission in permissions)
            claims.Add(new Claim(AuthClaimTypes.Permissions, permission));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static string[] GetPermissionsForSet(string permissionSet)
    {
        return permissionSet.ToLowerInvariant() switch
        {
            "admin" =>
            [
                Permissions.AssignIncidents,
                Permissions.ManageIncidents,
                Permissions.ReadIncidents,
                Permissions.ReadUsers,
                Permissions.CreateIncidents,
            ],
            "responder" =>
            [
                Permissions.ManageIncidents,
                Permissions.ReadIncidents,
                Permissions.ReadUsers,
            ],
            "viewer" =>
            [
                Permissions.ReadIncidents,
                Permissions.ReadUsers,
            ],
            _ => []
        };
    }
}