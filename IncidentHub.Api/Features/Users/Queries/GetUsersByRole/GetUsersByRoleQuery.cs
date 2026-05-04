using System.Text.Json;
using IncidentHub.Api.Contracts;
using IncidentHub.Api.Infrastructure.Data;
using MediatR;

namespace IncidentHub.Api.Features.Users.Queries.GetUsersByRole;

public record GetUsersByRoleQuery(string Role) : IRequest<IReadOnlyList<UserDto>>;

public class GetUsersByRoleQueryHandler(
    AppDbContext db,
    ILogger<GetUsersByRoleQueryHandler> logger,
    IConfiguration configuration)
    : IRequestHandler<GetUsersByRoleQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(
        GetUsersByRoleQuery request, CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Fetching users from Auth0 with role {Role}", request.Role);

            var domain = configuration["Auth0:Domain"];
            var clientId = configuration["Auth0:ClientId"];
            var clientSecret = configuration["Auth0:ClientSecret"];

            // Validate required configuration
            if (string.IsNullOrEmpty(domain) ||
                string.IsNullOrEmpty(clientId) ||
                string.IsNullOrEmpty(clientSecret))
            {
                throw new Exception($"Auth0 Missing - Domain: {domain ?? "NULL"}, ClientId: {clientId ?? "NULL"}, ClientSecret: {"DummySecret" ?? "NULL"}");
            }

            // Get management API token
            using var client = new HttpClient();
            var tokenResponse = await client.PostAsync($"https://{domain}/oauth/token",
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("audience", $"https://{domain}/api/v2/"),
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                }), ct);

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync(ct);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            var tokenResponseObj = JsonSerializer.Deserialize<Auth0TokenResponse>(tokenContent, options);

            if (tokenResponseObj?.AccessToken == null)
            {
                throw new Exception("Failed to get Auth0 management API token");
            }

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponseObj.AccessToken);

            // First, get the role ID by role name
            var rolesEndpoint = $"https://{domain}/api/v2/roles?name_filter={request.Role}";
            var rolesResponse = await client.GetAsync(rolesEndpoint, ct);
            var rolesContent = await rolesResponse.Content.ReadAsStringAsync(ct);
            var roles = JsonSerializer.Deserialize<List<Auth0Role>>(rolesContent, options);

            var role = roles?.FirstOrDefault(r => r.Name == request.Role);
            if (role == null || string.IsNullOrEmpty(role.Id))
            {
                logger.LogWarning("Role {Role} not found in Auth0", request.Role);
                return new List<UserDto>();
            }

            // Then, get all users for that role
            var usersEndpoint = $"https://{domain}/api/v2/roles/{role.Id}/users";
            var usersResponse = await client.GetAsync(usersEndpoint, ct);
            var usersContent = await usersResponse.Content.ReadAsStringAsync(ct);
            var users = JsonSerializer.Deserialize<List<Auth0User>>(usersContent, options);


            var userDtos = users?
                .Where(u => !string.IsNullOrEmpty(u.UserId))
                .Select(u => new UserDto
                {
                    Id = u.UserId!,
                    Name = !string.IsNullOrEmpty(u.Name) ? u.Name : u.Nickname ?? "Unknown",
                    Nickname = !string.IsNullOrEmpty(u.Nickname) ? u.Nickname : u.Email ?? "Unknown",
                    Email = u.Email ?? string.Empty,
                    Picture = u.Picture
                }).ToList() ?? new List<UserDto>();

            logger.LogInformation("Retrieved {Count} users with role {Role} from Auth0", userDtos.Count, request.Role);
            return userDtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching users with role {Role} from Auth0", request.Role);
            throw;
        }
    }
}