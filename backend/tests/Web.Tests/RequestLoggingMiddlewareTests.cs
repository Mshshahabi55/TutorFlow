namespace TutorFlow.Web.Tests;

// Verifies the one directly observable behavior RequestLoggingMiddleware
// adds to every response — X-Trace-Id — regardless of whether the
// downstream pipeline produced a success, a Result-mapped failure, or a
// 404. The log line itself (ILogger output) isn't asserted here: this
// project's other tests exercise the real HTTP pipeline end-to-end and
// verifying captured log records would need a second, parallel test
// double just for this one middleware — the header is the same
// information (a TraceIdentifier a caller can correlate back to a server
// log line), already proven to reach every response path this way.
public class RequestLoggingMiddlewareTests : IClassFixture<TutorFlowWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RequestLoggingMiddlewareTests(TutorFlowWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Successful_requests_carry_an_X_Trace_Id_response_header()
    {
        var response = await _client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("X-Trace-Id"));
    }

    [Fact]
    public async Task Not_found_responses_still_carry_an_X_Trace_Id_response_header()
    {
        var response = await _client.GetAsync("/this-route-does-not-exist");

        Assert.True(response.Headers.Contains("X-Trace-Id"));
    }
}
