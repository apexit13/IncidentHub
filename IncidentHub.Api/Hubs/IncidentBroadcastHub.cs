using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IncidentHub.Api.Hubs;

/// <summary>
/// Events pushed by handlers:
///   "IncidentRaised"      → IncidentDto
///   "IncidentUpdated"     → IncidentDto
///   "IncidentResolved"    → IncidentDto
///   "TimelineEntryAdded"  → TimelineEntryDto
/// </summary>
//[Authorize]
public class IncidentBroadcastHub : Hub
{
    // No methods needed for the current feature set.
    // If you later want clients to send messages to the server
    // (e.g. "user is typing a status update"), add them here.
}  
