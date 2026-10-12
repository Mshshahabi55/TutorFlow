namespace TutorFlow.Shared;

/// <summary>
/// ADR-0014 pattern: the read-only cross-module contract <c>Notifications</c> depends on via DI
/// to resolve the minimal tutor identity needed for a transactional email (display name, photo,
/// and the public profile slug a CTA can safely link back to) — implemented by <c>Tutors</c>,
/// the sole owner of this data. Deliberately narrower than <c>GetCurrentApplication.Response</c>
/// — never exposes internal application state (Status, RejectionReason, etc.) to a consumer
/// that only ever needs to compose an email.
/// </summary>
public interface ITutorProfileSummaryQuery
{
    Task<TutorProfileSummary?> GetAsync(Guid tutorUserId, CancellationToken ct);
}

public sealed record TutorProfileSummary(string DisplayName, string? ProfilePhotoReference, string? PublicSlug);
