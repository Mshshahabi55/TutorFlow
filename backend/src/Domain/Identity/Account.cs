using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.Events;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity;

// The registered identity of a Student, Tutor, Parent/Guardian, or
// Admin/Staff, specialized per role (DOMAIN_MODEL.md: Aggregates;
// docs/database/DOMAIN_DATA_MODEL.md, Section 3). Email and PasswordHash are
// the one personal-data field pair established so far — the login credential
// (docs/adr/ADR-017-authentication-mechanism-decision.md) — required from
// construction onward so an Account can never exist without one; no other
// personal data field is modeled here (PRODUCT_REQUIREMENTS.md Section 10.5,
// Item 17 remains otherwise open). Lockout state is intrinsic Account state,
// shared uniformly by all four roles, not a separate concept — it protects
// exactly the credential this aggregate already owns.
public abstract class Account : AggregateRoot<AccountId>
{
    // Ratified by docs/adr/ADR-017-authentication-mechanism-decision.md's
    // Addendum, Decision A (2026-07-21) — a bounding security parameter,
    // same category as AuthToken.SlidingWindow.
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    protected Account(AccountId id, EmailAddress email, PasswordHash passwordHash) : base(id)
    {
        Email = Guard.Against.Null(email, nameof(email));
        PasswordHash = Guard.Against.Null(passwordHash, nameof(passwordHash));
    }

    public EmailAddress Email { get; private set; }

    public PasswordHash PasswordHash { get; private set; }

    public int FailedLoginAttemptCount { get; private set; }

    public DateTime? LockedUntilUtc { get; private set; }

    public bool IsLocked(DateTime nowUtc) => LockedUntilUtc is not null && LockedUntilUtc > nowUtc;

    // Admin-assisted password reset only for this release — no self-service
    // recovery flow exists yet (docs/adr/ADR-017-authentication-mechanism-decision.md).
    // Also clears any lockout: a credential compromise severe enough to
    // warrant an Admin resetting the password is exactly the situation where
    // continuing to honor an old lockout window serves no purpose, and every
    // existing session is separately invalidated by the orchestrating use
    // case (Application layer, since that spans the AuthToken aggregate too).
    public void ResetPassword(PasswordHash newPasswordHash)
    {
        PasswordHash = Guard.Against.Null(newPasswordHash, nameof(newPasswordHash));
        FailedLoginAttemptCount = 0;
        LockedUntilUtc = null;
        RaiseDomainEvent(new PasswordReset(Id));
    }

    // Called once per wrong-password attempt against this specific Account
    // (LoginCommandHandler) — never for an email matching no Account at all,
    // since there is no Account to record the attempt against.
    public void RecordFailedLoginAttempt(DateTime nowUtc)
    {
        if (IsLocked(nowUtc))
        {
            return;
        }

        FailedLoginAttemptCount++;
        if (FailedLoginAttemptCount >= MaxFailedLoginAttempts)
        {
            LockedUntilUtc = nowUtc.Add(LockoutDuration);
            RaiseDomainEvent(new AccountLocked(Id, LockedUntilUtc.Value));
        }
    }

    public void RecordSuccessfulLogin()
    {
        FailedLoginAttemptCount = 0;
        LockedUntilUtc = null;
    }
}
