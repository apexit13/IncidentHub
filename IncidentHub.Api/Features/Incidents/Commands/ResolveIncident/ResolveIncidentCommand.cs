using System.Text.Json.Serialization;
using FluentValidation;
using IncidentHub.Api.Contracts;
using IncidentHub.Api.Domain;
using IncidentHub.Api.Domain.Enums;
using IncidentHub.Api.Hubs;
using IncidentHub.Api.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IncidentHub.Api.Features.Incidents.Commands.ResolveIncident;

public record ResolveIncidentCommand(
    string? ResolutionMessage
) : IRequest<IncidentDto>
{
    [JsonIgnore]
    public Guid Id { get; init; }

    [JsonIgnore]
    public string? ChangedBy { get; init; }
}

public class ResolveIncidentCommandHandler(
    AppDbContext db,
    IHubContext<IncidentBroadcastHub> hub,
    ILogger<ResolveIncidentCommandHandler> logger)
    : IRequestHandler<ResolveIncidentCommand, IncidentDto>
{
    public async Task<IncidentDto> Handle(
        ResolveIncidentCommand request, CancellationToken ct)
    {
        var incident = await db.Incidents
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Incident {request.Id} not found.");

        if (incident.Status == IncidentStatus.Resolved)
            throw new InvalidOperationException(
                $"Incident {request.Id} is already resolved.");

        incident.Status = IncidentStatus.Resolved;
        incident.ResolvedAt = DateTimeOffset.UtcNow;

        db.IncidentTimelines.Add(new IncidentTimeline
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            Message = request.ResolutionMessage ?? "Incident resolved.",
            ChangedBy = request.ChangedBy,
            Timestamp = DateTimeOffset.UtcNow,
            NewStatus = IncidentStatus.Resolved
        });

        await db.SaveChangesAsync(ct);

        var dto = incident.ToDto();
        try
        {
            await hub.Clients.All.SendAsync("IncidentResolved", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "SignalR broadcast failed for resolved incident {Id}", incident.Id);
        }

        return dto;
    }
}

public class ResolveIncidentCommandValidator : AbstractValidator<ResolveIncidentCommand>
{
    public ResolveIncidentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.ResolutionMessage)
            .MaximumLength(1000)
            .When(x => x.ResolutionMessage is not null);

        RuleFor(x => x.ChangedBy)
            .MaximumLength(500)
            .When(x => x.ChangedBy is not null);
    }
}
