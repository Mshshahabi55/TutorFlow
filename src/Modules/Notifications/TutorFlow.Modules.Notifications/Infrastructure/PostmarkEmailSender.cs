using System.IO;
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
            // No confirmed response was ever received — DNS failure, connection refused/reset, a
            // TLS failure, or (per .NET's own HttpClient behavior) the connection dropping while
            // the response was being read after the request had already been fully sent. None of
            // these prove Postmark never received or never queued the request; only that this
            // call never got to see a confirmed outcome. ADR-0029 Addendum A (Batch 55): this
            // must never be treated as a confirmed non-delivery.
            //
            // Deliberate omission: OperationCanceledException (caller cancellation, or an
            // HttpClient-internal timeout surfacing as TaskCanceledException) is NOT caught here
            // — it is left to propagate, exactly like every other cancellation path in this
            // feature. Converting it into any EmailSendResult (even Ambiguous) would require the
            // caller to treat cancellation as "a send attempt happened," which it must not.
            return EmailSendResult.Ambiguous($"Postmark request transport failure: {ex.Message}");
        }

        if (response.IsSuccessStatusCode)
        {
            return EmailSendResult.Sent();
        }

        var statusCode = (int)response.StatusCode;

        // Postmark's error body carries an ErrorCode/Message, never the email content —
        // included for operability, but the caller must still never log this string alongside
        // the recipient's address (see SendAbandonedCheckoutRecoveryEmail.Handler).
        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            // A status line was received (so some server responded with statusCode), but the
            // connection failed before the body could be read — the same uncertainty as a
            // transport failure on the request itself above: this failure does not rule out the
            // email having already been queued/sent server-side, regardless of which status code
            // was on the status line. Never a confirmed outcome (Batch 59).
            return EmailSendResult.Ambiguous($"Postmark response body read failed for status {statusCode}: {ex.Message}");
        }

        // A full HTTP response was received, so the request definitely reached Postmark and was
        // evaluated — the question is only what that evaluation proved. Postmark's own
        // documentation: a 4xx (e.g. 422) means "Postmark successfully received your request, but
        // the data inside that request is invalid" — a synchronous, pre-delivery rejection that
        // happens before any send is attempted, so it is safe to treat as confirmed non-delivery.
        //
        // 408 Request Timeout is excluded from that range (Batch 57): by definition it means the
        // server stopped waiting for a request it had not yet fully received, the opposite of "was
        // evaluated" — and in practice it is also one of the HTTP statuses most commonly generated
        // by an intermediary (load balancer, reverse proxy) in front of Postmark rather than by
        // Postmark's own application layer. Neither case establishes confirmed non-delivery.
        //
        // A 5xx is explicitly the opposite case in Postmark's own words: "in most cases the
        // message is lost during the process" — i.e. Postmark itself does not claim to know
        // whether the email went out, so this (and anything else outside the confirmed-rejection
        // range above) is classified as ambiguous, not a confirmed failure.
        return statusCode is >= 400 and < 500 and not 408
            ? EmailSendResult.ConfirmedNotSent($"Postmark returned {statusCode}: {body}")
            : EmailSendResult.Ambiguous($"Postmark returned {statusCode}: {body}");
    }
}
