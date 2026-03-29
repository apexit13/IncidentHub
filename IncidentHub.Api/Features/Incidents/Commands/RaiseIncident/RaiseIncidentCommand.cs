using FluentValidation;
using IncidentHub.Api.Contracts;
using IncidentHub.Api.Domain;
using IncidentHub.Api.Domain.Enums;
using IncidentHub.Api.Hubs;
using IncidentHub.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Severity = IncidentHub.Api.Domain.Enums.Severity;

namespace IncidentHub.Api.Features.Incidents.Commands.RaiseIncident;

public record RaiseIncidentCommand(
    string Title,
    string? Description,
    Severity Severity,
    string? AssignedTo
) : IRequest<IncidentDto>;

public class RaiseIncidentCommandHandler(
    AppDbContext db,
    IHubContext<IncidentBroadcastHub> hub,
    ILogger<RaiseIncidentCommandHandler> logger)
    : IRequestHandler<RaiseIncidentCommand, IncidentDto>
{
    public async Task<IncidentDto> Handle(
        RaiseIncidentCommand request, CancellationToken ct)
    {
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Severity = request.Severity,
            Status = IncidentStatus.New,
            AssignedTo = request.AssignedTo,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Incidents.Add(incident);
        db.IncidentTimelines.Add(new IncidentTimeline
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            Message = "Incident raised",
            Timestamp = DateTimeOffset.UtcNow,
            NewStatus = IncidentStatus.New
        });

        await db.SaveChangesAsync(ct);

        var dto = incident.ToDto();
        try
        {
            await hub.Clients.All.SendAsync("IncidentRaised", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "SignalR broadcast failed for incident {Id}", incident.Id);
        }

        return dto;
    }
}

public class RaiseIncidentCommandValidator : AbstractValidator<RaiseIncidentCommand>
{
    public RaiseIncidentCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(250)
            .WithMessage("Title must not exceed 250 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Severity)
            .IsInEnum()
            .WithMessage("Severity must be a valid value: Low, Medium, High, or Critical.");

        RuleFor(x => x.AssignedTo)
            .MaximumLength(500)
            .When(x => x.AssignedTo is not null);
    }
}
