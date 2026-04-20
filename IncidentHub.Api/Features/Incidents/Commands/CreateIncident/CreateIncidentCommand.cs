using FluentValidation;
using IncidentHub.Api.Contracts;
using IncidentHub.Api.Domain;
using IncidentHub.Api.Domain.Enums;
using IncidentHub.Api.Hubs;
using IncidentHub.Api.Infrastructure.Data;
using IncidentHub.Api.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Severity = IncidentHub.Api.Domain.Enums.Severity;

namespace IncidentHub.Api.Features.Incidents.Commands.CreateIncident;

public record CreateIncidentCommand(
    string Title,
    string? Description,
    Severity Severity,
    string? AssignedTo = null
) : IRequest<IncidentDto>;


public class CreateIncidentCommandHandler(
    AppDbContext db,
    IHubContext<IncidentBroadcastHub> hub,
    ICurrentUserService currentUserService,
    ILogger<CreateIncidentCommandHandler> logger)
    : IRequestHandler<CreateIncidentCommand, IncidentDto>
{
    public async Task<IncidentDto> Handle(
        CreateIncidentCommand request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Raising new incident: {Title}", request.Title);

            var changedBy = currentUserService.UserId;

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

            // Create timeline entry
            var timelineEntry = new IncidentTimeline
            {
                Id = Guid.NewGuid(),
                IncidentId = incident.Id,
                Message = $"Incident '{incident.Title}' raised with {incident.Severity} severity",
                Timestamp = DateTimeOffset.UtcNow,
                ChangedBy = changedBy,
                NewStatus = IncidentStatus.New
            };

            db.IncidentTimelines.Add(timelineEntry);

            await db.SaveChangesAsync(ct);

            var incidentDto = incident.ToDto();
            var timelineEntryDto = timelineEntry.ToDto();

            // Broadcast both events
            try
            {
                await hub.Clients.All.SendAsync("IncidentRaised", incidentDto, ct);
                await hub.Clients.All.SendAsync("TimelineEntryAdded", timelineEntryDto, ct);
                logger.LogInformation("Incident {Id} raised and broadcast successfully", incident.Id);
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
            logger.LogError(ex, "Failed to raise incident: {Title}", request.Title);
            throw;
        }
    }
}

public class CreateIncidentCommandValidator : AbstractValidator<CreateIncidentCommand>
{
    public CreateIncidentCommandValidator()
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