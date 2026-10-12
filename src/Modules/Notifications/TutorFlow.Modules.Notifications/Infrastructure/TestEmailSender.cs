using System.Collections.Concurrent;

namespace TutorFlow.Modules.Notifications.Infrastructure;

/// <summary>
/// Used only when no Postmark server token is configured — local dev and the automated test
/// suite (ADR-0029, mirroring <c>Payments.TestPaymentProviderClient</c>'s identical role).
/// Deterministic (always succeeds, records every message) so the rest of the pipeline — the
/// idempotency row, the handler's recipient/content-resolution logic — is exercised exactly as
/// it would be against the real provider. Registered scoped like every other handler
/// dependency in this codebase, but test fixtures that need to inspect captured sends register
/// a single shared instance instead (see <c>NotificationsApiFixture</c>) — a
/// <see cref="ConcurrentBag{T}"/> rather than a plain <c>List&lt;T&gt;</c> because Wolverine
/// message handling is not guaranteed single-threaded. This is swappable infrastructure behind
/// <see cref="IEmailSender"/>, not a product-logic shortcut.
/// </summary>
internal sealed class TestEmailSender : IEmailSender
{
    public ConcurrentBag<EmailMessage> SentMessages { get; } = [];

    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct)
    {
        SentMessages.Add(message);
        return Task.FromResult(EmailSendResult.Success());
    }
}
