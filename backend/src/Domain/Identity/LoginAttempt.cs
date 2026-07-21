using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.Events;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity;

// Exists solely to carry an auditable record of one login attempt through
// this codebase's existing Domain-Event-driven, same-transaction audit
// mechanism (docs/adr/ADR-016-audit-durability-strategy.md;
// docs/adr/ADR-017-authentication-mechanism-decision.md) — it has no other
// business behavior and is never queried or read back by any use case.
// A failed login (wrong password, or an email matching no Account) has no
// other aggregate to raise an event from, so this one exists uniformly for
// both the success and failure path rather than special-casing either.
public sealed class LoginAttempt : AggregateRoot<LoginAttemptId>
{
    private LoginAttempt(LoginAttemptId id, string email, bool succeeded, AccountId? accountId) : base(id)
    {
        Email = email;
        Succeeded = succeeded;
        AccountId = accountId;
    }

    public string Email { get; }

    public bool Succeeded { get; }

    public AccountId? AccountId { get; }

    public static LoginAttempt Record(string email, bool succeeded, AccountId? accountId)
    {
        var attempt = new LoginAttempt(LoginAttemptId.New(), email, succeeded, accountId);
        attempt.RaiseDomainEvent(new LoginAttempted(attempt.Id, email, succeeded, accountId));
        return attempt;
    }
}
