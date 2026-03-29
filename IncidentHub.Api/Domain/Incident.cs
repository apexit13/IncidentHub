using IncidentHub.Api.Domain.Enums;

namespace IncidentHub.Api.Domain;

public class Incident
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public Severity Severity { get; set; }
    public IncidentStatus Status { get; set; }
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Always stored and serialised as UTC.
    /// Use DateTimeOffset — not DateTime — so the JSON output includes
    /// the +00:00 offset and the React frontend can parse it unambiguously
    /// with new Date(isoString) regardless of the user's local timezone.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    // Navigation property — EF loads this via Include() or explicit load
    public ICollection<IncidentTimeline> Timeline { get; set; } = [];
}
