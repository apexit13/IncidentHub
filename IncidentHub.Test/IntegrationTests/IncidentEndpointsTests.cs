using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IncidentHub.Api.Features.Incidents.Commands.CreateIncident;
using IncidentHub.Tests.TestHelpers;

namespace IncidentHub.Tests.IntegrationTests;

public class IncidentEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── GET /api/incidents ────────────────────────────────────────────────

    [Fact]
    public async Task GetIncidents_ReturnsOk_ForAdminRole()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "admin");

        var response = await _client.GetAsync("/api/incidents", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetIncidents_ReturnsUnauthorized_WithoutPermissions()
    {
        _client.DefaultRequestHeaders.Clear();

        var response = await _client.GetAsync("/api/incidents", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/incidents/{id} ───────────────────────────────────────────

    [Fact]
    public async Task GetIncidentById_ReturnsNotFound_ForNonExistentId()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "admin");

        var response = await _client.GetAsync(
            $"/api/incidents/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/incidents ───────────────────────────────────────────────

    [Fact]
    public async Task CreateIncident_ReturnsCreated_WithValidData()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "admin");

        var createCommand = new CreateIncidentCommand(
            "Integration Test Incident",
            "Created via integration test",
            Severity.High);

        var response = await _client.PostAsJsonAsync(
            "/api/incidents",
            createCommand,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var incident = await response.Content.ReadFromJsonAsync<IncidentDto>(
            TestContext.Current.CancellationToken);
        incident.Should().NotBeNull();
        incident!.Title.Should().Be("Integration Test Incident");
    }

    [Fact]
    public async Task CreateIncident_ReturnsForbidden_ForViewerRole()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "viewer");

        var createCommand = new CreateIncidentCommand(
            "Test Incident",
            "Test description",
            Severity.Medium);

        var response = await _client.PostAsJsonAsync(
            "/api/incidents",
            createCommand,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}