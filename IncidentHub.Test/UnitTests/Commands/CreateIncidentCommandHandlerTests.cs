using IncidentHub.Api.Features.Incidents.Commands.CreateIncident;
using IncidentHub.Api.Hubs;
using IncidentHub.Api.Infrastructure.Data;
using IncidentHub.Api.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace IncidentHub.Tests.UnitTests.Commands;

public class CreateIncidentCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IHubContext<IncidentBroadcastHub>> _mockHubContext;
    private readonly Mock<ILogger<CreateIncidentCommandHandler>> _mockLogger;

    public CreateIncidentCommandHandlerTests()
    {
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockHubContext = new Mock<IHubContext<IncidentBroadcastHub>>();
        _mockLogger = new Mock<ILogger<CreateIncidentCommandHandler>>();

        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user-id");

        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
    }

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task Handle_ValidCommand_CreatesIncidentAndTimelineEntry()
    {
        using var context = CreateContext();
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

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Title.Should().Be("Test Incident");
        result.Severity.Should().Be("High");
        result.AssignedTo.Should().Be("responder-1");

        var savedIncident = await context.Incidents.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        savedIncident.Should().NotBeNull();
        savedIncident!.Title.Should().Be("Test Incident");

        var timelineEntry = await context.IncidentTimelines.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        timelineEntry.Should().NotBeNull();
        timelineEntry!.Message.Should().Contain("Test Incident");
        timelineEntry.ChangedBy.Should().Be("test-user-id");
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsDefaultStatusToNew()
    {
        using var context = CreateContext();
        var handler = new CreateIncidentCommandHandler(
            context,
            _mockHubContext.Object,
            _mockCurrentUserService.Object,
            _mockLogger.Object);

        var command = new CreateIncidentCommand(
            "Test Incident",
            "Test Description",
            Severity.Medium);

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Status.Should().Be("New");
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsCreatedAt()
    {
        using var context = CreateContext();
        var handler = new CreateIncidentCommandHandler(
            context,
            _mockHubContext.Object,
            _mockCurrentUserService.Object,
            _mockLogger.Object);

        var command = new CreateIncidentCommand(
            "Test Incident",
            "Test Description",
            Severity.Low);

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}