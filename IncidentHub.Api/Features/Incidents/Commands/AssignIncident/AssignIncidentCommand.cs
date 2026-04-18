using FluentValidation;
using IncidentHub.Api.Contracts;
using IncidentHub.Api.Domain;
using IncidentHub.Api.Hubs;
using IncidentHub.Api.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IncidentHub.Api.Features.Incidents.Commands.AssignIncident;

public record AssignIncidentCommand(
    string Id,
    string AssignedTo,
    string? ChangedBy = null
) : IRequest<IncidentDto>;

public class AssignIncidentCommandHandler(
    AppDbContext db,
    IHubContext<IncidentBroadcastHub> hub,
    ILogger<AssignIncidentCommandHandler> logger)
    : IRequestHandler<AssignIncidentCommand, IncidentDto>
{
    public async Task<IncidentDto> Handle(
        AssignIncidentCommand request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Assigning incident {Id} to {AssignedTo}", request.Id, request.AssignedTo);

            var incident = await db.Incidents
                .FirstOrDefaultAsync(i => i.Id == Guid.Parse(request.Id), ct);

            if (incident == null)
            {
                throw new Exception($"Incident with ID {request.Id} not found");
            }

            var previousAssignedTo = incident.AssignedTo;
            incident.AssignedTo = string.IsNullOrEmpty(request.AssignedTo) ? null : request.AssignedTo;

            // Create timeline entry
            var message = string.IsNullOrEmpty(request.AssignedTo)
                ? "Incident unassigned"
                : $"Incident assigned to {request.AssignedTo}";

            var timelineEntry = new IncidentTimeline
            {
                Id = Guid.NewGuid(),
                IncidentId = incident.Id,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow,
                ChangedBy = request.ChangedBy ?? "system",
                NewStatus = incident.Status
            };

            db.IncidentTimelines.Add(timelineEntry);

            await db.SaveChangesAsync(ct);

            // Map to DTOs using ToDto() methods
            var incidentDto = incident.ToDto();
            var timelineEntryDto = timelineEntry.ToDto();

            // Broadcast both events
            try
            {
                await hub.Clients.All.SendAsync("IncidentUpdated", incidentDto, ct);
                await hub.Clients.All.SendAsync("TimelineEntryAdded", timelineEntryDto, ct);
                logger.LogInformation("Incident {Id} assigned and broadcast successfully", incident.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "SignalR broadcast failed for incident {Id}", incident.Id);
            }

            return incidentDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to assign incident {Id}", request.Id);
            throw;
        }
    }
}

public class AssignIncidentCommandValidator : AbstractValidator<AssignIncidentCommand>
{
    public AssignIncidentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Incident ID is required.")
            .Must(id => Guid.TryParse(id, out _))
            .WithMessage("Incident ID must be a valid GUID.");

        RuleFor(x => x.AssignedTo)
            .MaximumLength(500)
            .When(x => x.AssignedTo is not null);
    }
}