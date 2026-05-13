using IncidentHub.Api.Features.Incidents.Queries.GetIncidents;
using IncidentHub.Api.Infrastructure.Data;
using IncidentHub.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IncidentHub.Tests.UnitTests.Queries;

public class GetIncidentsQueryHandlerTests
{
    private readonly ILogger<GetIncidentsQueryHandler> _logger;

    public GetIncidentsQueryHandlerTests()
    {
        _logger = new NullLogger<GetIncidentsQueryHandler>();
    }

    private static AppDbContext CreateContext() =>
    new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    [Fact]
    public async Task Handle_ReturnsAllIncidents()
    {
        // Arrange
        using var context = CreateContext();

        // Add test incidents
        var incident1 = TestDataFactory.CreateIncident("Incident 1", Severity.Critical);
        var incident2 = TestDataFactory.CreateIncident("Incident 2", Severity.High);
        context.Incidents.AddRange(incident1, incident2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add the logger parameter
        var handler = new GetIncidentsQueryHandler(context, _logger);
        var query = new GetIncidentsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(x => x.Title == "Incident 1");
        result.Should().Contain(x => x.Title == "Incident 2");
    }

    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateContext();
        // Add the logger parameter
        var handler = new GetIncidentsQueryHandler(context, _logger);
        var query = new GetIncidentsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsIncidentsWithCorrectMappings()
    {
        // Arrange
        using var context = CreateContext();

        var incident = TestDataFactory.CreateIncident(
            "Test Incident",
            Severity.High,
            IncidentStatus.Investigating,
            "test-responder");
        context.Incidents.Add(incident);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add the logger parameter
        var handler = new GetIncidentsQueryHandler(context, _logger);
        var query = new GetIncidentsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Title.Should().Be("Test Incident");
        dto.Severity.Should().Be("High");
        dto.Status.Should().Be("Investigating");
        dto.AssignedTo.Should().Be("test-responder");
    }
}