using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TutorFlow.Host.Tests;

public class HealthEndpointTests
{
    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
