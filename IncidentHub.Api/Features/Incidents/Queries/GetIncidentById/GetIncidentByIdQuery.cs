using IncidentHub.Api.Contracts;
using IncidentHub.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IncidentHub.Api.Features.Incidents.Queries.GetIncidentById;

public record GetIncidentByIdQuery(Guid Id) : IRequest<IncidentDto?>;

public class GetIncidentByIdQueryHandler(AppDbContext db)
    : IRequestHandler<GetIncidentByIdQuery, IncidentDto?>
{
    public async Task<IncidentDto?> Handle(
        GetIncidentByIdQuery request, CancellationToken ct)
    {
        var incident = await db.Incidents
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        return incident?.ToDto();
    }
}