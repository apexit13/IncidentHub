using IncidentHub.Api.Features.Incidents.Commands.UpdateIncidentStatus;
using IncidentHub.Api.Hubs;
using IncidentHub.Api.Infrastructure.Data;
using IncidentHub.Api.Infrastructure.Services;
using IncidentHub.Tests.TestHelpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IncidentHub.Tests.UnitTests.Commands;

public class UpdateIncidentStatusCommandHandlerTests
{
    private readonly DbContextOptions<AppDbContext> _dbContextOptions;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IHubContext<IncidentBroadcastHub>> _mockHubContext;
    private readonly ILogger<UpdateIncidentStatusCommandHandler> _logger;

    public UpdateIncidentStatusCommandHandlerTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UpdateStatusTestDb")
            .Options;

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockHubContext = new Mock<IHubContext<IncidentBroadcastHub>>();
        _logger = new NullLogger<UpdateIncidentStatusCommandHandler>();

        _mockCurrentUserService.Setup(x => x.UserId).Returns("test-user-id");
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesStatusAndCreatesTimeline()
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);

        var incident = TestDataFactory.CreateIncident(status: IncidentStatus.New);
        context.Incidents.Add(incident);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateIncidentStatusCommandHandler(
            context,
            _mockHubContext.Object,
            _mockCurrentUserService.Object,
            _logger);

        // Add the required Message parameter
        var command = new UpdateIncidentStatusCommand(IncidentStatus.Investigating, "Investigating now")
        {
            Id = incident.Id
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be("Investigating");

        // Verify incident was updated in database
        var updatedIncident = await context.Incidents.FindAsync([incident.Id], TestContext.Current.CancellationToken);
        updatedIncident!.Status.Should().Be(IncidentStatus.Investigating);

        // Verify timeline entry was created
        var timelineEntry = await context.IncidentTimelines.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        timelineEntry.Should().NotBeNull();
        timelineEntry!.NewStatus.Should().Be(IncidentStatus.Investigating);
        timelineEntry.Message.Should().Contain("Investigating now");
    }

    [Theory]
    [InlineData(IncidentStatus.New)]
    [InlineData(IncidentStatus.Investigating)]
    [InlineData(IncidentStatus.Identified)]
    [InlineData(IncidentStatus.Monitoring)]
    public async Task Handle_ValidCommand_UpdatesToAnyNonResolvedStatus(IncidentStatus newStatus)
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);

        var incident = TestDataFactory.CreateIncident(status: IncidentStatus.New);
        context.Incidents.Add(incident);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateIncidentStatusCommandHandler(
            context,
            _mockHubContext.Object,
            _mockCurrentUserService.Object,
            _logger);

        // Add the required Message parameter
        var command = new UpdateIncidentStatusCommand(newStatus, $"Status changed to {newStatus}")
        {
            Id = incident.Id
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(newStatus.ToString());
    }

    [Fact]
    public void Validator_RejectsResolvedStatus()
    {
        var validator = new UpdateIncidentStatusCommandValidator();
        var command = new UpdateIncidentStatusCommand(IncidentStatus.Resolved, null) { Id = Guid.NewGuid() };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("resolve endpoint"));
    }

    [Fact]
    public async Task Handle_ResolveStatus_UpdatesIncidentAndSetsResolvedAt()
    {
        using var context = new AppDbContext(_dbContextOptions);

        var incident = TestDataFactory.CreateIncident();
        context.Incidents.Add(incident);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateIncidentStatusCommandHandler(
            context,
            _mockHubContext.Object,
            _mockCurrentUserService.Object,
            _logger);

        var command = new UpdateIncidentStatusCommand(IncidentStatus.Resolved, "Resolving incident")
        {
            Id = incident.Id
        };

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.Status.Should().Be("Resolved");

        var updatedIncident = await context.Incidents.FindAsync([incident.Id], TestContext.Current.CancellationToken);
        updatedIncident!.ResolvedAt.Should().NotBeNull();
    }
}