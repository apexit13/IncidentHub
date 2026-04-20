// Features/Users/Queries/Auth0Records.cs
using System.Text.Json.Serialization;

namespace IncidentHub.Api.Features.Users.Queries;

public record Auth0TokenResponse
{
    public string? AccessToken { get; set; }
    public string? TokenType { get; set; }
    public int? ExpiresIn { get; set; }
    public string? Scope { get; set; }
}

public record Auth0Role
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public record Auth0User
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
    public string? Name { get; set; }
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Picture { get; set; }
}