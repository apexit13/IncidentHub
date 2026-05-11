using IncidentHub.Api.Features.Incidents.Commands.AssignIncident;
using IncidentHub.Api.Hubs;
using IncidentHub.Api.Infrastructure.Data;
using IncidentHub.Api.Infrastructure.Services;
using IncidentHub.Tests.TestHelpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IncidentHub.Tests.UnitTests.Commands;

public class AssignIncidentCommandHandlerTests
{
    private readonly DbContextOptions<AppDbContext> _dbContextOptions;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IHubContext<IncidentBroadcastHub>> _mockHubContext;
    private readonly Mock<IUserDisplayNameService> _mockUserDisplayNameService;
    private readonly ILogger<AssignIncidentCommandHandler> _logger;

    public AssignIncidentCommandHandlerTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique per test
            .Options;

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockHubContext = new Mock<IHubContext<IncidentBroadcastHub>>();
        _mockUserDisplayNameService = new Mock<IUserDisplayNameService>();
        _logger = new NullLogger<AssignIncidentCommandHandler>();

        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user-id");
        _mockUserDisplayNameService.Setup(x => x.GetDisplayNameAsync(It.IsAny<string>()))
            .ReturnsAsync("Test User");

        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
    }
    [Fact]
    public async Task Handle_ValidCommand_UpdatesAssignmentAndCreatesTimeline()
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);

        _mockUserDisplayNameService
            .Setup(s => s.GetDisplayNameAsync("new-responder"))
            .ReturnsAsync("New Responder");

        var incident = TestDataFactory.CreateIncident();
        context.Incidents.Add(incident);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignIncidentCommandHandler(
            context,
            _mockHubContext.Object,
            _mockCurrentUserService.Object,
            _mockUserDisplayNameService.Object,
            _logger);

        var command = new AssignIncidentCommand("new-responder") { Id = incident.Id };

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.AssignedTo.Should().Be("new-responder");

        var updatedIncident = await context.Incidents.FindAsync([incident.Id], TestContext.Current.CancellationToken);
        updatedIncident!.AssignedTo.Should().Be("new-responder");

        var timelineEntry = await context.IncidentTimelines.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        timelineEntry.Should().NotBeNull();
        timelineEntry!.Message.Should().Contain("assigned to");
        timelineEntry.ChangedBy.Should().Be("test-user-id");
    }

    [Fact]
    public async Task Handle_UnassignCommand_SetsAssignedToNull()
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);

        // Create incident with existing assignment
        var incident = TestDataFactory.CreateIncident(assignedTo: "existing-responder");
        context.Incidents.Add(incident);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AssignIncidentCommandHandler(
            context,
            _mockHubContext.Object,
            _mockCurrentUserService.Object,
            _mockUserDisplayNameService.Object,
            _logger);

        var command = new AssignIncidentCommand(null) { Id = incident.Id };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.AssignedTo.Should().BeNull();

        // Verify timeline entry shows unassignment
        var timelineEntry = await context.IncidentTimelines.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        timelineEntry!.Message.Should().Contain("unassigned");
    }

    [Fact]
    public async Task Handle_NonExistentIncident_ThrowsException()
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);
        var handler = new AssignIncidentCommandHandler(
            context,
            _mockHubContext.Object,
            _mockCurrentUserService.Object,
            _mockUserDisplayNameService.Object,
            _logger);

        var command = new AssignIncidentCommand("responder") { Id = Guid.NewGuid() };

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}