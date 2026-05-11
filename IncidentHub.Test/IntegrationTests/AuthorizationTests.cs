using System.Net;
using FluentAssertions;
using IncidentHub.Tests.TestHelpers;

namespace IncidentHub.Tests.IntegrationTests;

public class AuthorizationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── Unauthorized ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("/api/incidents", "GET")]
    [InlineData("/api/incidents/00000000-0000-0000-0000-000000000000", "GET")]
    [InlineData("/api/incidents", "POST")]
    [InlineData("/api/incidents/00000000-0000-0000-0000-000000000000/status", "PATCH")]
    [InlineData("/api/incidents/00000000-0000-0000-0000-000000000000/resolve", "POST")]
    [InlineData("/api/incidents/00000000-0000-0000-0000-000000000000/assignment", "PATCH")]
    public async Task Endpoints_ReturnUnauthorized_WithoutAuthentication(string url, string method)
    {
        _client.DefaultRequestHeaders.Clear();
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/incidents ────────────────────────────────────────────────

    [Theory]
    [InlineData("admin")]
    [InlineData("responder")]
    [InlineData("viewer")]
    public async Task GetIncidents_ReturnsOk_ForAllRoles(string role)
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", role);

        var response = await _client.GetAsync("/api/incidents", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/incidents/{id} ───────────────────────────────────────────

    [Fact]
    public async Task GetIncidentById_ReturnsNotFound_ForUnknownId()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "admin");

        var response = await _client.GetAsync(
            "/api/incidents/00000000-0000-0000-0000-000000000000",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetIncidentById_ReturnsNotFound_ForViewerRole()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "viewer");

        var response = await _client.GetAsync(
            "/api/incidents/00000000-0000-0000-0000-000000000000",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/incidents ───────────────────────────────────────────────

    [Fact]
    public async Task CreateIncident_ReturnsForbidden_ForViewerRole()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "viewer");

        var content = new StringContent(
            "{\"title\":\"Test Incident\",\"severity\":\"Medium\",\"description\":\"Test\"}",
            System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/incidents", content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateIncident_ReturnsCreated_ForAdminRole()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "admin");

        var content = new StringContent(
            "{\"title\":\"Test Incident\",\"severity\":\"Medium\",\"description\":\"Test\"}",
            System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/incidents", content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── PATCH /api/incidents/{id}/status ─────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ReturnsForbidden_ForViewerRole()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "viewer");

        var content = new StringContent(
            "{\"status\":\"Investigating\"}",
            System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PatchAsync(
            "/api/incidents/00000000-0000-0000-0000-000000000000/status",
            content,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNotFound_ForResponderRole()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "responder");

        var content = new StringContent(
            "{\"status\":\"Investigating\"}",
            System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PatchAsync(
            "/api/incidents/00000000-0000-0000-0000-000000000000/status",
            content,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/incidents/{id}/resolve ─────────────────────────────────

    [Fact]
    public async Task ResolveIncident_ReturnsForbidden_ForViewerRole()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "viewer");

        var response = await _client.PostAsync(
            "/api/incidents/00000000-0000-0000-0000-000000000000/resolve",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── PATCH /api/incidents/{id}/assignment ──────────────────────────────

    [Fact]
    public async Task UpdateAssignment_ReturnsForbidden_ForViewerRole()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "viewer");

        var content = new StringContent(
            "{\"assignedTo\":\"auth0|test-user-id\"}",
            System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PatchAsync(
            "/api/incidents/00000000-0000-0000-0000-000000000000/assignment",
            content,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateAssignment_ReturnsNotFound_ForAdminRole()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Permissions", "admin");

        var content = new StringContent(
            "{\"assignedTo\":\"auth0|test-user-id\"}",
            System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PatchAsync(
            "/api/incidents/00000000-0000-0000-0000-000000000000/assignment",
            content,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}