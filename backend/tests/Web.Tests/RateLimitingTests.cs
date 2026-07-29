using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TutorFlow.Web.Tests;

// The shared TutorFlowWebApplicationFactory deliberately configures very
// high rate-limit permits (see its own comment) so the other ~250 tests in
// this project — which legitimately fire many requests, including repeated
// /auth/login calls, from one shared client IP per test class — are never
// throttled. This file is the one place that opts back into the real, tight
// limits, via WithWebHostBuilder on a *derived* factory instance scoped to
// each test, so nothing here affects any other test class.
public class RateLimitingTests : IClassFixture<TutorFlowWebApplicationFactory>
{
    private readonly TutorFlowWebApplicationFactory _factory;

    public RateLimitingTests(TutorFlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private HttpClient CreateClientWithTightAuthLimit(int permitLimit) =>
        _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Auth:PermitLimit"] = permitLimit.ToString(),
                    ["RateLimiting:Auth:WindowSeconds"] = "60",
                    // General must stay above Auth's own limit here, or the
                    // global limiter (not the one this test means to
                    // exercise) would be the one to reject first.
                    ["RateLimiting:General:PermitLimit"] = "100000",
                    ["RateLimiting:General:WindowSeconds"] = "10",
                })))
            .CreateClient();

    private HttpClient CreateClientWithTightGeneralLimit(int permitLimit) =>
        _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:General:PermitLimit"] = permitLimit.ToString(),
                    ["RateLimiting:General:WindowSeconds"] = "60",
                })))
            .CreateClient();

    // Wrong credentials on purpose — this proves the *rate limiter*, not
    // LoginCommandHandler's own success path, is what produces the eventual
    // 429; every one of these individual attempts would otherwise fail with
    // its own ordinary Domain-error 200/failure response.
    private static Task<HttpResponseMessage> AttemptLoginAsync(HttpClient client) =>
        client.PostAsJsonAsync("/auth/login", new { Email = "nobody@example.com", Password = "wrong" });

    [Fact]
    public async Task Login_beyond_the_auth_policy_limit_is_rejected_with_429_and_the_standard_ApiResponse_envelope()
    {
        var client = CreateClientWithTightAuthLimit(permitLimit: 2);

        // Wrong-credentials attempts fail with their own ordinary
        // (non-429) status — only the third one, past the permit limit,
        // should ever see 429.
        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await AttemptLoginAsync(client)).StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await AttemptLoginAsync(client)).StatusCode);
        var throttled = await AttemptLoginAsync(client);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.True(throttled.Headers.RetryAfter is not null || throttled.Headers.Contains("Retry-After"));

        var body = await ReadBodyAsync(throttled);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
        Assert.Equal("RateLimit.TooManyRequests", body.GetProperty("error").GetProperty("code").GetString());
        // ErrorType.Infrastructure = 2 (TutorFlow.Application.Common.ErrorType) —
        // see RateLimitingRegistration's own comment on why this is the
        // closest existing fit rather than a new category.
        Assert.Equal(2, body.GetProperty("error").GetProperty("type").GetInt32());
    }

    [Fact]
    public async Task Login_within_the_auth_policy_limit_is_never_throttled()
    {
        var client = CreateClientWithTightAuthLimit(permitLimit: 3);

        var third = await AttemptLoginAsync(client);
        await AttemptLoginAsync(client);

        Assert.NotEqual(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task Requests_beyond_the_general_policy_limit_are_rejected_with_429_regardless_of_endpoint()
    {
        var client = CreateClientWithTightGeneralLimit(permitLimit: 2);

        (await client.GetAsync("/health")).EnsureSuccessStatusCode();
        (await client.GetAsync("/health")).EnsureSuccessStatusCode();
        var throttled = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
    }
}
