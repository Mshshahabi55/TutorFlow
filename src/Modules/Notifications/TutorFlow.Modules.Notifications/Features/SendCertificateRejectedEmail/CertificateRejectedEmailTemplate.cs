using System.Net;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Notifications.Features.SendCertificateRejectedEmail;

/// <summary>Original TutorFlow copy — no Preply evidence exists for this specific email (the
/// supplied screenshots show only the in-wizard "we're reviewing your documents" banner, not a
/// rejection notification), so nothing here is modeled on an observed Preply template. Rejection
/// reason is shown verbatim (the one piece of decision detail a tutor-facing email should carry),
/// never any other internal review metadata.</summary>
internal static class CertificateRejectedEmailTemplate
{
    public static EmailMessage Build(string recipientEmail, TutorCertificateRejected rejected, string appBaseUrl)
    {
        var baseUrl = appBaseUrl.TrimEnd('/');
        var ctaUrl = $"{baseUrl}/become-a-tutor";
        var certificateName = WebUtility.HtmlEncode(rejected.CertificateName);
        var reason = WebUtility.HtmlEncode(rejected.RejectionReason);

        var subject = $"Your \"{rejected.CertificateName}\" certificate needs attention";

        var htmlBody = $"""
            <!DOCTYPE html>
            <html>
              <body style="margin:0;padding:0;background-color:#f5f5f5;font-family:Arial,Helvetica,sans-serif;color:#1a1a1a;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;margin:0 auto;background:#ffffff;">
                  <tr><td style="padding:24px 24px 0 24px;font-size:20px;font-weight:bold;">TutorFlow</td></tr>
                  <tr>
                    <td style="padding:16px 24px;">
                      <h1 style="font-size:18px;margin:0 0 16px 0;">Your "{certificateName}" certificate needs attention</h1>
                      <p style="margin:0 0 16px 0;font-size:15px;">
                        Our team reviewed the document you uploaded for this certificate and couldn't verify it:
                      </p>
                      <p style="margin:0 0 24px 0;padding:12px 16px;background:#fff5f5;border-left:3px solid #e6007e;font-size:14px;">
                        {reason}
                      </p>
                      <p style="margin:0 0 24px 0;font-size:15px;">
                        You can upload a replacement document any time from your tutor profile.
                      </p>
                      <a href="{WebUtility.HtmlEncode(ctaUrl)}" style="display:inline-block;background:#e6007e;color:#ffffff;text-decoration:none;padding:12px 24px;border-radius:4px;font-weight:bold;">
                        Go to your profile
                      </a>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;

        var textBody = $"""
            Your "{rejected.CertificateName}" certificate needs attention

            Our team reviewed the document you uploaded for this certificate and couldn't verify it:

            {rejected.RejectionReason}

            You can upload a replacement document any time from your tutor profile: {ctaUrl}
            """;

        return new EmailMessage(recipientEmail, subject, htmlBody, textBody);
    }
}
