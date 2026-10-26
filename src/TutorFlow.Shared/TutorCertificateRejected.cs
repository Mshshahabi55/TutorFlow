namespace TutorFlow.Shared;

/// <summary>Published by <c>Tutors</c> whenever an admin rejects a pending certificate document
/// (Stage 2 — Certification). <c>Notifications</c> consumes it to email the tutor. Deliberately
/// carries no file reference or storage detail — the rejection reason is the only decision detail
/// a tutor-facing email needs, same allowlist discipline as <c>PublicCertificateEntry</c>.</summary>
public sealed record TutorCertificateRejected(
    Guid TutorUserId,
    Guid ApplicationId,
    Guid CertificateId,
    string CertificateName,
    string RejectionReason,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}
