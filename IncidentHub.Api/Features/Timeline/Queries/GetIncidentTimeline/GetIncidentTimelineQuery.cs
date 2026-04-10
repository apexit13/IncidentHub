using IncidentHub.Api.Contracts;
using IncidentHub.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IncidentHub.Api.Features.Timeline.Queries.GetIncidentTimeline;

public record GetIncidentTimelineQuery(Guid IncidentId) : IRequest<IReadOnlyList<TimelineEntryDto>>;

public class GetIncidentTimelineQueryHandler(
    AppDbContext db,
    ILogger<GetIncidentTimelineQueryHandler> logger)
    : IRequestHandler<GetIncidentTimelineQuery, IReadOnlyList<TimelineEntryDto>>
{
    public async Task<IReadOnlyList<TimelineEntryDto>> Handle(
        GetIncidentTimelineQuery request, CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Fetching timeline for incident {IncidentId}", request.IncidentId);

            var timelineEntries = await db.IncidentTimelines
                .Where(t => t.IncidentId == request.IncidentId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync(ct);

            logger.LogInformation("Retrieved {Count} timeline entries for incident {IncidentId}",
                timelineEntries.Count, request.IncidentId);

            return timelineEntries.Select(t => t.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching timeline for incident {IncidentId}", request.IncidentId);
            throw;
        }
    }
}