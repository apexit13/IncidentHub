using System.Net;
using FluentAssertions;
using IncidentHub.Tests.TestHelpers;

namespace IncidentHub.Tests.IntegrationTests;

public class UnauthorizedTests(IncidentHubTestFactory factory) : IClassFixture<IncidentHubTestFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/api/incidents", "GET")]
    [InlineData("/api/incidents/00000000-0000-0000-0000-000000000000", "GET")]
    public async Task ReadEndpoints_ReturnUnauthorized_WithoutAuthentication(string url, string method)
    {
        _client.DefaultRequestHeaders.Clear();
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/incidents", "POST")]
    public async Task WriteEndpoints_ReturnUnauthorized_WithoutAuthentication(string url, string method)
    {
        _client.DefaultRequestHeaders.Clear();
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (method == "POST")
        {
            request.Content = new StringContent("{\"title\":\"Test\",\"severity\":\"Medium\"}",
                System.Text.Encoding.UTF8, "application/json");
        }

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}