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

public sealed record EmailSendResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static EmailSendResult Success() => new(true);

    public static EmailSendResult Failure(string errorMessage) => new(false, errorMessage);
}
