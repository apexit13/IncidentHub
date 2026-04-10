using IncidentHub.Api.Contracts;
using IncidentHub.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IncidentHub.Api.Features.Incidents.Queries.GetIncidents;

public record GetIncidentsQuery() : IRequest<IReadOnlyList<IncidentDto>>;

public class GetIncidentsQueryHandler(
    AppDbContext db,
    ILogger<GetIncidentsQueryHandler> logger)
    : IRequestHandler<GetIncidentsQuery, IReadOnlyList<IncidentDto>>
{
    public async Task<IReadOnlyList<IncidentDto>> Handle(
        GetIncidentsQuery request, CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Fetching all incidents");

            var incidents = await db.Incidents
                .OrderByDescending(i => i.Severity)
                .ThenByDescending(i => i.CreatedAt)
                .ToListAsync(ct);

            logger.LogInformation("Retrieved {Count} incidents", incidents.Count);
            return incidents.Select(i => i.ToDto()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching incidents");
            throw;
        }
    }
}