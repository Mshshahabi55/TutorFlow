using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Notifications.Features.SendCertificateRejectedEmail;

/// <summary>Reacts to <see cref="TutorCertificateRejected"/>. Deliberately simpler than
/// <c>SendAbandonedCheckoutRecoveryEmail</c>'s claim/lease/ownership-fencing machinery — that
/// ceremony exists there because a duplicate send risked a confusing double-notification tied to
/// a financial/booking flow with strict idempotency requirements. A duplicate certificate-rejection
/// email is a minor inconvenience, not a correctness issue, so this follows the proportionate,
/// simpler shape: resolve the recipient, send, log the outcome, let an unexpected failure
/// propagate to Wolverine's own standard retry/error-queue behavior rather than inventing a
/// second bespoke idempotency mechanism for a low-stakes notification.</summary>
public static class SendCertificateRejectedEmail
{
    public sealed class Handler(IEmailSender emailSender, IUserEmailQuery userEmailQuery, IOptions<NotificationsOptions> options, ILogger<Handler> logger)
    {
        public async Task HandleAsync(TutorCertificateRejected message, CancellationToken ct)
        {
            var recipientEmail = await userEmailQuery.GetEmailAsync(message.TutorUserId, ct);
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                logger.LogInformation(
                    "Certificate-rejected email skipped for tutor {TutorUserId}: no email on file", message.TutorUserId);
                return;
            }

            var email = CertificateRejectedEmailTemplate.Build(recipientEmail, message, options.Value.AppBaseUrl);
            var result = await emailSender.SendAsync(email, ct);

            switch (result.Outcome)
            {
                case EmailDeliveryOutcome.Sent:
                    logger.LogInformation("Certificate-rejected email sent for tutor {TutorUserId}, certificate {CertificateId}", message.TutorUserId, message.CertificateId);
                    break;
                case EmailDeliveryOutcome.ConfirmedNotSent:
                    logger.LogWarning("Certificate-rejected email failed to send for tutor {TutorUserId}, certificate {CertificateId}", message.TutorUserId, message.CertificateId);
                    break;
                case EmailDeliveryOutcome.Ambiguous:
                default:
                    logger.LogWarning(
                        "Certificate-rejected email send outcome for tutor {TutorUserId}, certificate {CertificateId} could not be determined ({ErrorMessage})",
                        message.TutorUserId, message.CertificateId, result.ErrorMessage);
                    break;
            }
        }
    }
}
