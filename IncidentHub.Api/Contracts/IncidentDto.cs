
namespace IncidentHub.Api.Contracts;

public record IncidentDto(
    Guid Id,
    string Title,
    string? Description,
    string Severity,
    string Status,
    string? AssignedTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt
);
