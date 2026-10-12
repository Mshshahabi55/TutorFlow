using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace TutorFlow.Modules.Notifications.Infrastructure;

/// <summary>
/// Real Postmark integration (ADR-0029) — a plain server-to-server REST call via
/// <see cref="IHttpClientFactory"/> (never <c>new HttpClient()</c>), the same external-API-call
/// shape <c>Bookings.PaymentsApiClient</c> already established. No Postmark SDK/NuGet package is
/// used — the REST contract is tiny enough that a typed client plus a plain JSON payload avoids
/// one more third-party dependency, mirroring how <c>StripePaymentProviderClient</c> isolates
/// its vendor behind <c>IPaymentProviderClient</c>. The server-token header is set per
/// <see cref="HttpRequestMessage"/>, never via <c>DefaultRequestHeaders</c> on the pooled,
/// factory-managed client (which is shared across requests and must never be mutated).
/// </summary>
internal sealed class PostmarkEmailSender(IHttpClientFactory httpClientFactory, IOptions<NotificationsOptions> options) : IEmailSender
{
    public const string HttpClientName = "Postmark";

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct)
    {
        var opts = options.Value;
        var client = httpClientFactory.CreateClient(HttpClientName);

        var payload = new
        {
            From = string.IsNullOrWhiteSpace(opts.FromName) ? opts.FromEmail : $"{opts.FromName} <{opts.FromEmail}>",
            To = message.ToEmail,
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody,
            MessageStream = "outbound"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "email")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-Postmark-Server-Token", opts.PostmarkServerToken);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            return EmailSendResult.Failure($"Postmark request failed: {ex.Message}");
        }

        if (response.IsSuccessStatusCode)
        {
            return EmailSendResult.Success();
        }

        // Postmark's error body carries an ErrorCode/Message, never the email content —
        // included for operability, but the caller must still never log this string alongside
        // the recipient's address (see SendAbandonedCheckoutRecoveryEmail.Handler).
        var body = await response.Content.ReadAsStringAsync(ct);
        return EmailSendResult.Failure($"Postmark returned {(int)response.StatusCode}: {body}");
    }
}
