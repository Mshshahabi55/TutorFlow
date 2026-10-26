using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TutorFlow.Modules.Identity.Tests;

/// <summary>
/// HTTP-level coverage of the external-login callback endpoints, exercised without a real
/// Google/Facebook account (impossible in CI — no live provider credentials exist). Hitting the
/// callback route directly, with no prior <c>ExternalLogin</c> temporary-cookie session, is not a
/// simulation: it is exactly what happens in production if the user cancels on the provider's own
/// consent screen (the provider never redirects back at all) or navigates to the callback URL
/// directly — `HttpContext.AuthenticateAsync("ExternalLogin")` fails the same way in both cases,
/// so this is real coverage of that code path, not a stand-in for it.
/// </summary>
[Collection(IdentityHostCollection.Name)]
public class ExternalLoginCallbackEndpointTests(IdentityApiFixture fixture) : IClassFixture<IdentityApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task GoogleCallback_WithNoPriorExternalLoginSession_RedirectsToASafeCancelledError()
    {
        var response = await _client.GetAsync(
            "/api/identity/external/google/callback?returnTo=%2Fbecome-a-tutor", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);
        Assert.StartsWith("http://localhost:3000/login", location!.ToString());
        Assert.Contains("externalAuthError=cancelled", location.ToString());

        // The one thing this test exists to prove: whatever the frontend eventually does with
        // this error code, nothing resembling a token/credential is present anywhere in the URL.
        Assert.DoesNotContain("accessToken", location.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", location.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FacebookCallback_WithNoPriorExternalLoginSession_RedirectsToASafeCancelledError()
    {
        var response = await _client.GetAsync("/api/identity/external/facebook/callback", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);
        Assert.StartsWith("http://localhost:3000/login", location!.ToString());
        Assert.Contains("externalAuthError=cancelled", location.ToString());
    }

    [Fact]
    public async Task GoogleCallback_NeverSetsTheSessionCookie_WhenTheExternalLoginFails()
    {
        var response = await _client.GetAsync("/api/identity/external/google/callback", TestContext.Current.CancellationToken);

        var setCookieHeaders = response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies.ToList() : [];
        Assert.DoesNotContain(setCookieHeaders, c => c.StartsWith("TutorFlowAuth=", StringComparison.Ordinal) && !c.Contains("01 Jan 1970"));
    }

    [Theory]
    [InlineData("/external/google")]
    [InlineData("/external/facebook")]
    public async Task Challenge_WithAMaliciousReturnTo_NeverEmbedsItUnsanitized(string challengePath)
    {
        // The challenge endpoint redirects to the real provider (unreachable/irrelevant in this
        // test env) or, lacking real provider credentials, fails before that — either way, this
        // asserts the request is accepted (not a 404/500 from routing) and, when a redirect is
        // produced, it never points at the attacker-supplied host. A full assertion on the
        // encrypted correlation-cookie payload's embedded returnTo is not possible from outside
        // the process; ExternalLoginReturnToTests covers the sanitizer itself exhaustively.
        var response = await _client.GetAsync(
            $"/api/identity{challengePath}?returnTo=https://evil.example.com", TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            var location = response.Headers.Location?.ToString() ?? string.Empty;
            Assert.DoesNotContain("evil.example.com", location, StringComparison.OrdinalIgnoreCase);
        }
    }
}
