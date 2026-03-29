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

    /// <summary>
    /// Captures the status the incident moved TO with this timeline entry.
    /// Null for the initial "Incident raised" entry.
    /// Allows the frontend to render status-change badges in the timeline
    /// without recomputing history from a diff.
    /// </summary>
    public IncidentStatus? NewStatus { get; set; }
}
