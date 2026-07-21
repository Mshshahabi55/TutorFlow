namespace TutorFlow.Web.Tests;

public class ObservabilityTests : IClassFixture<TutorFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly TutorFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ObservabilityTests(TutorFlowWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_endpoint_is_reachable_and_reports_healthy()
    {
        var response = await _client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task OpenApi_document_is_reachable_and_describes_the_endpoint_surface()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"openapi\"", body);
    }

    [Fact]
    public async Task OpenApi_document_includes_401_and_403_responses_for_auth_sensitive_endpoints()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"401\"", body);
        Assert.Contains("\"403\"", body);
    }

    [Fact]
    public async Task Unconfigured_cross_origin_request_is_rejected_by_the_CORS_policy()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://not-a-configured-origin.example");

        var response = await _client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
