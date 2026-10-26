namespace TutorFlow.Modules.Messaging;

/// <summary>
/// Messaging's own response-level projection of a Tutor's safe public identity — deliberately not
/// a reuse of <c>TutorFlow.Shared.TutorProfileSummary</c> as the literal wire type, so Messaging's
/// HTTP contract stays decoupled from that cross-module query's own internal shape (ADR-0030
/// Batch 69). Every field here is already served anonymously by `Tutors`' public
/// <c>GetPublicProfile</c> endpoint — no new privacy exposure. Never includes email, internal
/// application status, or any other private field.
/// </summary>
public sealed record TutorDisplaySummary(string DisplayName, string? ProfilePhotoReference, string? PublicSlug);
