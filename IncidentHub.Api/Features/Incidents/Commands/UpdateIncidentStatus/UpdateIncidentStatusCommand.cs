using FluentValidation;
using IncidentHub.Api.Contracts;
using IncidentHub.Api.Domain;
using IncidentHub.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using IncidentHub.Api.Hubs;
using Microsoft.EntityFrameworkCore;
using IncidentHub.Api.Domain.Enums;

namespace IncidentHub.Api.Features.Incidents.Commands.UpdateIncidentStatus;

// Id is set by the endpoint from the route parameter: cmd with { Id = id }
public record UpdateIncidentStatusCommand(
    Guid Id,
    IncidentStatus NewStatus,
    string? Message,
    string? ChangedBy
) : IRequest<IncidentDto>;

public class UpdateIncidentStatusCommandHandler(
    AppDbContext db,
    IHubContext<IncidentBroadcastHub> hub,
    ILogger<UpdateIncidentStatusCommandHandler> logger)
    : IRequestHandler<UpdateIncidentStatusCommand, IncidentDto>
{
    public async Task<IncidentDto> Handle(
        UpdateIncidentStatusCommand request, CancellationToken ct)
    {
        var incident = await db.Incidents
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Incident {request.Id} not found.");

        var previousStatus = incident.Status;
        incident.Status = request.NewStatus;

        if (request.NewStatus == IncidentStatus.Resolved)
            incident.ResolvedAt = DateTimeOffset.UtcNow;

        var timelineMessage = request.Message
            ?? $"Status changed from {previousStatus} to {request.NewStatus}";

        db.IncidentTimelines.Add(new IncidentTimeline
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            Message = timelineMessage,
            ChangedBy = request.ChangedBy,
            Timestamp = DateTimeOffset.UtcNow,
            NewStatus = request.NewStatus
        });

        await db.SaveChangesAsync(ct);

        var dto = incident.ToDto();
        var hubEvent = request.NewStatus == IncidentStatus.Resolved
            ? "IncidentResolved"
            : "IncidentUpdated";

        try
        {
            await hub.Clients.All.SendAsync(hubEvent, dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "SignalR broadcast failed for incident {Id} event {Event}",
                incident.Id, hubEvent);
        }

        return dto;
    }
}

public class UpdateIncidentStatusCommandValidator
    : AbstractValidator<UpdateIncidentStatusCommand>
{
    public UpdateIncidentStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .WithMessage("NewStatus must be a valid IncidentStatus value.");

        RuleFor(x => x.NewStatus)
            .NotEqual(IncidentStatus.Resolved)
            .WithMessage("Use the resolve endpoint to resolve an incident.");

        RuleFor(x => x.Message)
            .MaximumLength(1000)
            .When(x => x.Message is not null);

        RuleFor(x => x.ChangedBy)
            .MaximumLength(500)
            .When(x => x.ChangedBy is not null);
    }
}
