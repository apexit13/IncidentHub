using IncidentHub.Api.Contracts;
using IncidentHub.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IncidentHub.Api.Features.Timeline.Queries.GetIncidentTimeline;

public class GetIncidentTimelineQueryHandler(AppDbContext db)
    : IRequestHandler<GetIncidentTimelineQuery, IReadOnlyList<TimelineEntryDto>>
{
    public async Task<IReadOnlyList<TimelineEntryDto>> Handle(
        GetIncidentTimelineQuery request, CancellationToken ct)
    {
        var entries = await db.IncidentTimelines
            .Where(t => t.IncidentId == request.IncidentId)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(ct);

        return entries.Select(t => t.ToDto()).ToList();
    }
}