using System.Net;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Notifications.Features.SendAbandonedCheckoutRecoveryEmail;

/// <summary>
/// Replicates the OBSERVED information architecture of Preply's real "abandoned slot" recovery
/// email (`18-screenshot-evidence.md` SS51–SS52) — tutor identity, the specific slot date/time
/// with an explicit time-zone disclaimer, a single CTA back to the tutor — using original
/// TutorFlow copy and branding throughout. Preply's own wording ("X is ready for your lesson",
/// "Give it a try") is never reused verbatim.
///
/// Known, undisguised gap vs. the observed evidence: Preply's email states the time "based on
/// your location" (a personalized, per-recipient time zone). TutorFlow has no per-student time
/// zone preference stored anywhere in this codebase today — rather than fabricate one, this
/// template renders the slot in UTC with an explicit "(UTC)" label. Also, Preply's email shows
/// lesson/topic context; <c>Booking</c> has no subject/topic field at all (confirmed by
/// inspection), so none is rendered — never invented.
/// </summary>
internal static class AbandonedCheckoutEmailTemplate
{
    public static EmailMessage Build(string recipientEmail, TutorProfileSummary tutor, BookingHoldExpired booking, string appBaseUrl)
    {
        var baseUrl = appBaseUrl.TrimEnd('/');
        var ctaUrl = tutor.PublicSlug is not null ? $"{baseUrl}/tutors/{tutor.PublicSlug}" : baseUrl;
        var helpUrl = $"{baseUrl}/help";

        var tutorName = WebUtility.HtmlEncode(tutor.DisplayName);
        var slotDateLabel = booking.StartUtc.ToString("dddd, MMMM d, yyyy");
        var slotTimeLabel = booking.StartUtc.ToString("HH:mm");

        var subject = $"Your lesson slot with {tutor.DisplayName} is still open";

        var photoHtml = IsAbsoluteHttpsUrl(tutor.ProfilePhotoReference)
            ? $"""<img src="{WebUtility.HtmlEncode(tutor.ProfilePhotoReference)}" width="64" height="64" alt="{tutorName}" style="border-radius:50%;display:block;" />"""
            : "";

        var htmlBody = $"""
            <!DOCTYPE html>
            <html>
              <body style="margin:0;padding:0;background-color:#f5f5f5;font-family:Arial,Helvetica,sans-serif;color:#1a1a1a;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;margin:0 auto;background:#ffffff;">
                  <tr><td style="padding:24px 24px 0 24px;font-size:20px;font-weight:bold;">TutorFlow</td></tr>
                  <tr>
                    <td style="padding:16px 24px;">
                      <h1 style="font-size:18px;margin:0 0 16px 0;">Your lesson slot with {tutorName} is still open</h1>
                      <table role="presentation" cellpadding="0" cellspacing="0">
                        <tr>
                          <td style="padding-right:12px;">{photoHtml}</td>
                          <td>
                            <div style="font-weight:bold;">{tutorName}</div>
                            <div style="color:#555555;font-size:14px;">Reserved slot — not yet confirmed</div>
                          </td>
                        </tr>
                      </table>
                      <p style="margin:16px 0 4px 0;font-size:15px;">
                        <strong>{slotDateLabel}, {slotTimeLabel} (UTC)</strong>
                      </p>
                      <p style="margin:0 0 16px 0;color:#777777;font-size:13px;">Time shown in UTC — convert to your local time zone.</p>
                      <p style="margin:0 0 24px 0;font-size:15px;">
                        You started booking a lesson with {tutorName} but didn't finish. The slot is no longer held, but {tutorName} is still available — pick a new time whenever you're ready.
                      </p>
                      <a href="{WebUtility.HtmlEncode(ctaUrl)}" style="display:inline-block;background:#e6007e;color:#ffffff;text-decoration:none;padding:12px 24px;border-radius:4px;font-weight:bold;">
                        Continue to {tutorName}'s profile
                      </a>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:24px;border-top:1px solid #eeeeee;color:#999999;font-size:12px;">
                      You're receiving this email because of recent activity on your TutorFlow account.
                      <a href="{WebUtility.HtmlEncode(helpUrl)}" style="color:#999999;">Contact us</a>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;

        var textBody = $"""
            Your lesson slot with {tutor.DisplayName} is still open

            {slotDateLabel}, {slotTimeLabel} (UTC) — time shown in UTC.

            You started booking a lesson with {tutor.DisplayName} but didn't finish. The slot is no longer held, but {tutor.DisplayName} is still available — pick a new time whenever you're ready.

            Continue: {ctaUrl}

            You're receiving this email because of recent activity on your TutorFlow account.
            Contact us: {helpUrl}
            """;

        return new EmailMessage(recipientEmail, subject, htmlBody, textBody);
    }

    private static bool IsAbsoluteHttpsUrl(string? reference) =>
        !string.IsNullOrWhiteSpace(reference) && Uri.TryCreate(reference, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
