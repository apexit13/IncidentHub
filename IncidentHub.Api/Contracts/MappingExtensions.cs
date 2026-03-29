using IncidentHub.Api.Domain;

namespace IncidentHub.Api.Contracts;

public static class MappingExtensions
{
    public static IncidentDto ToDto(this Incident i) => new(
        i.Id,
        i.Title,
        i.Description,
        i.Severity.ToString(),
        i.Status.ToString(),
        i.AssignedTo,
        i.CreatedAt,
        i.ResolvedAt
    );

    public static TimelineEntryDto ToDto(this IncidentTimeline t) => new(
        t.Id,
        t.IncidentId,
        t.Message,
        t.ChangedBy,
        t.Timestamp,
        t.NewStatus?.ToString()
    );
}
