using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity;

// A server-side reference token backing one authenticated session
// (docs/adr/ADR-017-authentication-mechanism-decision.md). Deliberately its
// own aggregate root, not a child of Account: issuance/revocation needs no
// transactional consistency with Account mutations, and it is always looked
// up by token hash, never navigated to via its owning Account (mirrors why
// AvailabilitySlot/Session are separate aggregates from Tutor, ADR-015).
// Only the token's hash is ever held here or persisted — the raw token value
// exists solely in the response returned to the caller at issuance.
public sealed class AuthToken : AggregateRoot<AuthTokenId>
{
    // Fixed by ADR-017 (short-lived, sliding, no separate refresh token) —
    // not environment-configurable, since it is a decided security policy,
    // not a deployment concern.
    public static readonly TimeSpan SlidingWindow = TimeSpan.FromMinutes(30);

    // Absolute cap regardless of continued activity — ratified by ADR-017's
    // Addendum, Decision B (2026-07-21), which corrects this ADR's original
    // Token lifetime statement to reflect this already-implemented behavior.
    // Bounds how long a single session can be kept alive by sliding renewal
    // alone; a genuinely new session (a fresh login) is always available
    // once this is hit, so this is a ceiling on one continuous session, not
    // a limit on how often someone can sign in.
    public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromHours(12);

    private AuthToken(
        AuthTokenId id,
        AccountId accountId,
        string role,
        string tokenHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        DateTime absoluteExpiresAtUtc)
        : base(id)
    {
        AccountId = accountId;
        Role = role;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AbsoluteExpiresAtUtc = absoluteExpiresAtUtc;
    }

    public AccountId AccountId { get; }

    // Denormalized from the Account at issuance, deliberately: resolving
    // role requires checking all four role-specific Account tables (email is
    // unique per role, not globally — docs/adr/ADR-017-authentication-mechanism-decision.md),
    // which is acceptable once, at login, but not on every authenticated
    // request this token is presented on. A role never changes for an
    // existing Account, so this cannot drift.
    public string Role { get; }

    public string TokenHash { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime ExpiresAtUtc { get; private set; }

    // Immutable once issued — sliding renewal (ExtendExpiry) never moves
    // this forward, which is exactly how it bounds total session length.
    public DateTime AbsoluteExpiresAtUtc { get; }

    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsValid(DateTime nowUtc) =>
        RevokedAtUtc is null && ExpiresAtUtc > nowUtc && AbsoluteExpiresAtUtc > nowUtc;

    public static AuthToken Issue(AccountId accountId, string role, string tokenHash, DateTime nowUtc) =>
        new(AuthTokenId.New(), accountId, role, tokenHash, nowUtc, nowUtc.Add(SlidingWindow), nowUtc.Add(AbsoluteLifetime));

    // Called on every successful authenticated request that presents this
    // token (sliding expiration, ADR-017) — never on a token that has
    // already expired or been revoked. Capped at AbsoluteExpiresAtUtc: a
    // continuously active session still eventually needs a fresh login.
    public void ExtendExpiry(DateTime nowUtc)
    {
        if (!IsValid(nowUtc))
        {
            throw new InvalidOperationException("Cannot extend an expired or revoked token.");
        }

        var next = nowUtc.Add(SlidingWindow);
        ExpiresAtUtc = next < AbsoluteExpiresAtUtc ? next : AbsoluteExpiresAtUtc;
    }

    // Logout and revocation are the same operation (ADR-017 Sections 10-11).
    public void Revoke(DateTime nowUtc)
    {
        if (RevokedAtUtc is not null)
        {
            throw new InvalidOperationException("Token is already revoked.");
        }

        RevokedAtUtc = nowUtc;
    }
}
