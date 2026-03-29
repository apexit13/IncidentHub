namespace IncidentHub.Api.Contracts;

public record TimelineEntryDto(
    Guid Id,
    Guid IncidentId,
    string Message,
    string? ChangedBy,
    DateTimeOffset Timestamp,
    string? NewStatus
);
