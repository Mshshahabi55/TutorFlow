using System.Net;
using System.Net.Http.Json;

namespace TutorFlow.Modules.Identity.Tests;

[Collection(IdentityHostCollection.Name)]
public class AuthRateLimitingTests(AuthRateLimitingApiFixture fixture) : IClassFixture<AuthRateLimitingApiFixture>
{
    private HttpClient CreateClientForSimulatedIp(string simulatedIp)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Add(AuthRateLimitingApiFixture.SimulatedClientIpHeader, simulatedIp);
        return client;
    }

    private static string NewSimulatedIp() =>
        $"10.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}";

    [Fact]
    public async Task Register_WithinLimit_EachRequestStillSucceedsNormally()
    {
        using var client = CreateClientForSimulatedIp(NewSimulatedIp());

        for (var i = 0; i < AuthRateLimitingApiFixture.PermitLimit; i++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/identity/register",
                new { email = $"{Guid.NewGuid():N}@example.com", password = "P@ssword123" },
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    [Fact]
    public async Task Register_ExceedingLimit_ReturnsTooManyRequestsWithRetryAfter()
    {
        using var client = CreateClientForSimulatedIp(NewSimulatedIp());

        for (var i = 0; i < AuthRateLimitingApiFixture.PermitLimit; i++)
        {
            await client.PostAsJsonAsync(
                "/api/identity/register",
                new { email = $"{Guid.NewGuid():N}@example.com", password = "P@ssword123" },
                TestContext.Current.CancellationToken);
        }

        var response = await client.PostAsJsonAsync(
            "/api/identity/register",
            new { email = $"{Guid.NewGuid():N}@example.com", password = "P@ssword123" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.RetryAfter is not null, "429 response must carry a Retry-After header.");
    }

    [Fact]
    public async Task Login_WithinLimit_StillReturnsUnauthorizedForBadCredentials_NotMaskedByTheLimiter()
    {
        using var client = CreateClientForSimulatedIp(NewSimulatedIp());

        var response = await client.PostAsJsonAsync(
            "/api/identity/login",
            new { email = $"{Guid.NewGuid():N}@example.com", password = "WrongPassword1" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ExceedingLimit_ReturnsTooManyRequests_RegardlessOfCredentialCorrectness()
    {
        using var client = CreateClientForSimulatedIp(NewSimulatedIp());

        for (var i = 0; i < AuthRateLimitingApiFixture.PermitLimit; i++)
        {
            await client.PostAsJsonAsync(
                "/api/identity/login",
                new { email = $"{Guid.NewGuid():N}@example.com", password = "WrongPassword1" },
                TestContext.Current.CancellationToken);
        }

        var response = await client.PostAsJsonAsync(
            "/api/identity/login",
            new { email = $"{Guid.NewGuid():N}@example.com", password = "WrongPassword1" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task DifferentSimulatedClientIps_DoNotShareRateLimitCounters()
    {
        using var exhaustedClient = CreateClientForSimulatedIp(NewSimulatedIp());
        for (var i = 0; i < AuthRateLimitingApiFixture.PermitLimit; i++)
        {
            await exhaustedClient.PostAsJsonAsync(
                "/api/identity/register",
                new { email = $"{Guid.NewGuid():N}@example.com", password = "P@ssword123" },
                TestContext.Current.CancellationToken);
        }
        var exhaustedResponse = await exhaustedClient.PostAsJsonAsync(
            "/api/identity/register",
            new { email = $"{Guid.NewGuid():N}@example.com", password = "P@ssword123" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.TooManyRequests, exhaustedResponse.StatusCode);

        using var otherClient = CreateClientForSimulatedIp(NewSimulatedIp());
        var otherResponse = await otherClient.PostAsJsonAsync(
            "/api/identity/register",
            new { email = $"{Guid.NewGuid():N}@example.com", password = "P@ssword123" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, otherResponse.StatusCode);
    }
}
