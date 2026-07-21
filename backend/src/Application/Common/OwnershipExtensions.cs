namespace TutorFlow.Application.Common;

// Layer 2 (resource-instance/ownership) authorization, per ADR-003's
// two-tier model — evaluated in the Application layer of the owning
// context, never in Presentation or Infrastructure (ADR-003: Risks;
// AUTHORIZATION_MATRIX.md). Shared here because every WP4 Priority 3
// handler needs identical behavior: a client-supplied TutorId is never
// trusted on its own — it is always compared against the caller's own
// authenticated identity (ICurrentUserProvider), which
// AuthenticationMiddleware already established and Layer 1
// (RequirePermission) already required to hold the relevant coarse-grained
// permission before this check ever runs.
public static class OwnershipExtensions
{
    // Fine-grained handlers with no RequirePermission metadata (WP4 Priority
    // 5's read endpoints) never pass through AuthorizationMiddleware, so the
    // comment above's assumption — that Layer 1 already turned away an
    // unauthenticated caller — does not hold for them. Any such handler must
    // call this first so an anonymous caller still gets 401, not the 403 an
    // authenticated-but-forbidden caller gets from VerifyOwnTutorId/VerifyIsParty
    // below. One shared check, so the anonymous-vs-forbidden distinction is
    // decided in exactly one place.
    public static Error? VerifyAuthenticated(this ICurrentUserProvider currentUser, string errorCode) =>
        currentUser.IsAuthenticated
            ? null
            : new Error(
                errorCode,
                "Authentication is required to access this resource.",
                ErrorType.Authorization);

    public static Error? VerifyOwnTutorId(this ICurrentUserProvider currentUser, Guid tutorId, string errorCode) =>
        currentUser.CurrentAccountId() == tutorId
            ? null
            : new Error(
                errorCode,
                "You do not have permission to modify this Tutor's resource.",
                ErrorType.Authorization);

    // WP4 Priority 4: for resources with more than one legitimate party
    // (e.g. a Session's Tutor, Student, and optional Parent/Guardian; a
    // Relationship's two named parties) — the caller must be one of the
    // named ids, never trusted from the request alone. Null entries (an
    // optional party not present on this resource) are ignored.
    public static Error? VerifyIsParty(this ICurrentUserProvider currentUser, string errorCode, params Guid?[] partyIds)
    {
        var callerId = currentUser.CurrentAccountId();
        return partyIds.Any(id => id.HasValue && id.Value == callerId)
            ? null
            : new Error(
                errorCode,
                "You do not have permission to act on this resource.",
                ErrorType.Authorization);
    }
}
