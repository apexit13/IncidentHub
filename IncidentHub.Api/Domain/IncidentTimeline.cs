using IncidentHub.Api.Domain.Enums;

namespace IncidentHub.Api.Domain;

public class IncidentTimeline
{
    public Guid Id { get; set; }

    // Foreign key + navigation property
    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;

    public required string Message { get; set; }

    public string? ChangedBy { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public IncidentStatus? NewStatus { get; set; }
}
