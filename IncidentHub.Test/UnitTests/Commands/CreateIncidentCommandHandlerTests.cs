using IncidentHub.Api.Features.Incidents.Commands.CreateIncident;
using IncidentHub.Api.Hubs;
using IncidentHub.Api.Infrastructure.Data;
using IncidentHub.Api.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace IncidentHub.Tests.UnitTests.Commands;

public class CreateIncidentCommandHandlerTests
{
    private readonly DbContextOptions<AppDbContext> _dbContextOptions;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IHubContext<IncidentBroadcastHub>> _mockHubContext;
    private readonly Mock<ILogger<CreateIncidentCommandHandler>> _mockLogger;

    public CreateIncidentCommandHandlerTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("CreateIncidentTestDb")
            .Options;

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockHubContext = new Mock<IHubContext<IncidentBroadcastHub>>();
        _mockLogger = new Mock<ILogger<CreateIncidentCommandHandler>>();

        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user-id");
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesIncidentAndTimelineEntry()
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);
        var handler = new CreateIncidentCommandHandler(
            context,
            _mockHubContext.Object,
            _mockCurrentUserService.Object,
            _mockLogger.Object);

        var command = new CreateIncidentCommand(
            "Test Incident",
            "Test Description",
            Severity.High,
            "responder-1");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Incident");
        result.Severity.Should().Be("High");
        result.AssignedTo.Should().Be("responder-1");

        // Verify incident was saved
        var savedIncident = await context.Incidents.FirstOrDefaultAsync();
        savedIncident.Should().NotBeNull();
        savedIncident!.Title.Should().Be("Test Incident");

        // Verify timeline entry was created
        var timelineEntry = await context.IncidentTimelines.FirstOrDefaultAsync();
        timelineEntry.Should().NotBeNull();
        timelineEntry!.Message.Should().Contain("Test Incident");
        timelineEntry.ChangedBy.Should().Be("test-user-id");
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsDefaultStatusToNew()
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);
        var handler = new CreateIncidentCommandHandler(
            context,
            _mockHubContext.Object,
            _mockCurrentUserService.Object,
            _mockLogger.Object);

        var command = new CreateIncidentCommand(
            "Test Incident",
            "Test Description",
            Severity.Medium);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("New");
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsCreatedAt()
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);
        var handler = new CreateIncidentCommandHandler(
            context,
            _mockHubContext.Object,
            _mockCurrentUserService.Object,
            _mockLogger.Object);

        var command = new CreateIncidentCommand(
            "Test Incident",
            "Test Description",
            Severity.Low);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}