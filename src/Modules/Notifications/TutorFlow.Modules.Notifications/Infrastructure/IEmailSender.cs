namespace TutorFlow.Modules.Notifications.Infrastructure;

/// <summary>
/// Provider-neutral transactional email abstraction (ADR-0029) — the only seam
/// <c>Notifications</c>' application/domain layers ever see. No vendor SDK type appears in this
/// interface's signature; a provider-specific implementation (<see cref="PostmarkEmailSender"/>)
/// lives entirely in this <c>Infrastructure</c> namespace, swappable without touching any
/// feature handler.
/// </summary>
public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct);
}

public sealed record EmailMessage(string ToEmail, string Subject, string HtmlBody, string? PlainTextBody = null);

/// <summary>
/// Three outcomes, not two (ADR-0029 Addendum A, Batch 55 update) — a send attempt is not always
/// cleanly "succeeded" or "definitely failed". <see cref="EmailDeliveryOutcome.Ambiguous"/> exists
/// for exactly the case a boolean result cannot express: the provider may have already accepted
/// the email before this call could confirm either outcome (e.g. the connection dropped while
/// reading the response). A single <c>bool IsSuccess</c> cannot represent this without an
/// additional, independently-settable flag that could contradict it — this is the smallest
/// change that avoids that.
/// </summary>
public enum EmailDeliveryOutcome
{
    /// <summary>The provider confirmed it accepted the email.</summary>
    Sent,

    /// <summary>The provider's response proves it did not accept the email — safe to treat as a
    /// definitive, no-side-effect failure.</summary>
    ConfirmedNotSent,

    /// <summary>Whether the provider accepted the email could not be determined. Never a
    /// definitive outcome — callers must not record this as a terminal failure.</summary>
    Ambiguous
}

public sealed record EmailSendResult(EmailDeliveryOutcome Outcome, string? ErrorMessage = null)
{
    public static EmailSendResult Sent() => new(EmailDeliveryOutcome.Sent);

    public static EmailSendResult ConfirmedNotSent(string errorMessage) => new(EmailDeliveryOutcome.ConfirmedNotSent, errorMessage);

    public static EmailSendResult Ambiguous(string errorMessage) => new(EmailDeliveryOutcome.Ambiguous, errorMessage);
}
