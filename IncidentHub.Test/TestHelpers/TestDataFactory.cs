namespace IncidentHub.Tests.TestHelpers;

public static class TestDataFactory
{
    public static Incident CreateIncident(
        string title = "Test Incident",
        Severity severity = Severity.Medium,
        IncidentStatus status = IncidentStatus.New,
        string? assignedTo = null)
    {
        return new Incident
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test description",
            Severity = severity,
            Status = status,
            AssignedTo = assignedTo,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static IncidentTimeline CreateTimelineEntry(
        Guid incidentId,
        string message = "Test timeline entry",
        string? changedBy = "test-user",
        IncidentStatus? newStatus = null)
    {
        return new IncidentTimeline
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            Message = message,
            ChangedBy = changedBy,
            Timestamp = DateTimeOffset.UtcNow,
            NewStatus = newStatus
        };
    }

    public static UserDto CreateTestUser(string role = "admin")
    {
        return new UserDto
        {
            Id = "auth0|test-user-id",
            Name = "Test User",
            Nickname = "tester",
            Email = "test@example.com",
            Picture = null
        };
    }
}