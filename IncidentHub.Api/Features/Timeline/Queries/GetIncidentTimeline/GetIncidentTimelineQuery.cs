using IncidentHub.Api.Contracts;
using MediatR;

namespace IncidentHub.Api.Features.Timeline.Queries.GetIncidentTimeline;

public record GetIncidentTimelineQuery(Guid IncidentId) : IRequest<IReadOnlyList<TimelineEntryDto>>;