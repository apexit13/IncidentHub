using IncidentHub.Api.Contracts;
using IncidentHub.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IncidentHub.Api.Features.Incidents.Queries.GetIncidentById;

public record GetIncidentByIdQuery(Guid Id) : IRequest<IncidentDto?>;

public class GetIncidentByIdQueryHandler(
    AppDbContext db,
    ILogger<GetIncidentByIdQueryHandler> logger)
    : IRequestHandler<GetIncidentByIdQuery, IncidentDto?>
{
    public async Task<IncidentDto?> Handle(
        GetIncidentByIdQuery request, CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Fetching incident {Id}", request.Id);

            var incident = await db.Incidents
                .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

            if (incident == null)
            {
                logger.LogInformation("Incident {Id} not found", request.Id);
                return null;
            }

            logger.LogDebug("Incident {Id} found", request.Id);
            return incident.ToDto();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching incident {Id}", request.Id);
            throw;
        }
    }
}