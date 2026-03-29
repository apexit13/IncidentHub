using IncidentHub.Api.Contracts;
using IncidentHub.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IncidentHub.Api.Features.Incidents.Queries.GetIncidents;

public record GetIncidentsQuery() : IRequest<IReadOnlyList<IncidentDto>>;

public class GetIncidentsQueryHandler(AppDbContext db)
    : IRequestHandler<GetIncidentsQuery, IReadOnlyList<IncidentDto>>
{
    public async Task<IReadOnlyList<IncidentDto>> Handle(
        GetIncidentsQuery request, CancellationToken ct)
    {
        var incidents = await db.Incidents
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return incidents.Select(i => i.ToDto()).ToList();
    }
}