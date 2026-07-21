namespace TutorFlow.Application.Common;

// Every authorization check from here on (Launch Preparation, Priority 2)
// will need the caller's identity as a Guid to compare against a resource's
// owner id (e.g. Session.StudentId.Value) — ICurrentUserProvider.UserId
// stays string (Authentication's own, already-approved shape; not modified
// here) since it also flows into AuditEntry.ActorId as a string. Parsing it
// in every future handler would duplicate the same conversion repeatedly;
// this is the one place it happens instead.
public static class CurrentUserExtensions
{
    public static Guid? CurrentAccountId(this ICurrentUserProvider currentUser) =>
        currentUser.UserId is { } userId && Guid.TryParse(userId, out var accountId) ? accountId : null;
}
