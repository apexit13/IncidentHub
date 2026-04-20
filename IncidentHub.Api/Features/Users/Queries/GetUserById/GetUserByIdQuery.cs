using System.Text.Json;
using System.Text.Json.Serialization;
using IncidentHub.Api.Contracts;
using IncidentHub.Api.Infrastructure.Data;
using MediatR;

namespace IncidentHub.Api.Features.Users.Queries.GetUserById;

public record GetUserByIdQuery(string UserId) : IRequest<UserDto?>;

public class GetUserByIdQueryHandler(
        AppDbContext db,
        ILogger<GetUserByIdQueryHandler> logger,
        IConfiguration configuration)
        : IRequestHandler<GetUserByIdQuery, UserDto?>
    {
        public async Task<UserDto?> Handle(
            GetUserByIdQuery request, CancellationToken ct)
        {
            try
            {
                logger.LogDebug("Fetching user {UserId} from Auth0", request.UserId);

                var domain = configuration["Auth0:Domain"];
                var clientId = configuration["Auth0:ClientId"];
                var clientSecret = configuration["Auth0:ClientSecret"];

                // Validate required configuration
                if (string.IsNullOrEmpty(domain) ||
                    string.IsNullOrEmpty(clientId) ||
                    string.IsNullOrEmpty(clientSecret))
                {
                    throw new Exception("Auth0 configuration is missing required values");
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

                // Get user by ID
                var userEndpoint = $"https://{domain}/api/v2/users/{request.UserId}?fields=user_id%2Cname%2Cnickname%2Cemail%2Cpicture&include_fields=true";
                var userResponse = await client.GetAsync(userEndpoint, ct);

                if (!userResponse.IsSuccessStatusCode)
                {
                    logger.LogWarning("User {UserId} not found in Auth0", request.UserId);
                    return null;
                }

                var userContent = await userResponse.Content.ReadAsStringAsync(ct);
                var user = JsonSerializer.Deserialize<Auth0User>(userContent, options);

                if (user == null || string.IsNullOrEmpty(user.UserId))
                {
                    logger.LogWarning("User {UserId} not found in Auth0", request.UserId);
                    return null;
                }

                var userDto = new UserDto
                {
                    Id = request.UserId, 
                    Name = !string.IsNullOrEmpty(user.Name) ? user.Name : user.Nickname ?? "Unknown",
                    Nickname = !string.IsNullOrEmpty(user.Nickname) ? user.Nickname : user.Email ?? "Unknown",
                    Email = user.Email ?? string.Empty,
                    Picture = user.Picture
                };

                logger.LogInformation("Retrieved user {UserId} from Auth0", request.UserId);
                return userDto;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching user {UserId} from Auth0", request.UserId);
                throw;
            }
        }
    }