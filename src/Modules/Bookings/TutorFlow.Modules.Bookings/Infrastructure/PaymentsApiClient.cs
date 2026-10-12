using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace TutorFlow.Modules.Bookings.Infrastructure;

/// <summary>
/// The one and only way <c>Bookings</c> reaches <c>Payments</c> for checkout (ADR-0019,
/// ADR-0027) — a plain server-to-server HTTP call to <c>Payments</c>' own public API, via
/// <c>IHttpClientFactory</c> (never <c>new HttpClient()</c>, never DI of a <c>Payments</c> type).
/// This is the same mechanism an external client would use, and survives unchanged if
/// <c>Payments</c> is ever extracted into its own deployable.
///
/// <c>Payments</c>' endpoint still requires a valid <c>Student</c> JWT (ADR-0019 — the ordering,
/// not a new service-auth scheme, is the real boundary); this client forwards the *original
/// caller's* bearer token from the current request, read from either the auth cookie or the
/// Authorization header — the exact same two sources <c>TutorFlow.Host/Program.cs</c>'s own
/// <c>OnMessageReceived</c> already accepts. The cookie name is a deliberately duplicated literal
/// (matching this codebase's established per-module RoleNames/PolicyNames duplication
/// convention), not a reference to Identity's <c>AuthCookie</c> type.
/// </summary>
public sealed class PaymentsApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
{
    public const string HttpClientName = "Payments";
    private const string AuthCookieName = "TutorFlowAuth";

    public async Task<PaymentsCheckoutResult> CreatePaymentIntentAsync(Guid bookingId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);

        var token = httpContextAccessor.HttpContext?.Request.Cookies[AuthCookieName]
            ?? httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.PostAsJsonAsync("/api/payments/intents", new { bookingId }, ct);

        if (!response.IsSuccessStatusCode)
        {
            return PaymentsCheckoutResult.Failure();
        }

        var body = await response.Content.ReadFromJsonAsync<PaymentIntentResponse>(ct);
        return body is null
            ? PaymentsCheckoutResult.Failure()
            : PaymentsCheckoutResult.Success(body.PaymentReferenceId, body.ClientSecret);
    }

    private sealed record PaymentIntentResponse(Guid PaymentReferenceId, string ClientSecret);
}

public sealed record PaymentsCheckoutResult(bool IsSuccess, Guid? PaymentReferenceId, string? ClientSecret)
{
    public static PaymentsCheckoutResult Success(Guid paymentReferenceId, string clientSecret) => new(true, paymentReferenceId, clientSecret);
    public static PaymentsCheckoutResult Failure() => new(false, null, null);
}
