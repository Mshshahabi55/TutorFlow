using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Tests.Identity;

public class AuthTokenTests
{
    private static AuthToken IssueToken(DateTime nowUtc) =>
        AuthToken.Issue(AccountId.New(), "Tutor", "token-hash", nowUtc);

    [Fact]
    public void A_freshly_issued_token_is_valid()
    {
        var now = DateTime.UtcNow;
        var token = IssueToken(now);

        Assert.True(token.IsValid(now));
    }

    [Fact]
    public void ExtendExpiry_slides_the_expiry_forward_on_continued_activity()
    {
        var now = DateTime.UtcNow;
        var token = IssueToken(now);
        var later = now.Add(AuthToken.SlidingWindow).AddMinutes(-1);

        token.ExtendExpiry(later);

        Assert.True(token.IsValid(later));
    }

    [Fact]
    public void A_token_expires_once_the_sliding_window_elapses_with_no_activity()
    {
        var now = DateTime.UtcNow;
        var token = IssueToken(now);

        Assert.False(token.IsValid(now.Add(AuthToken.SlidingWindow).AddSeconds(1)));
    }

    [Fact]
    public void ExtendExpiry_never_moves_expiry_past_the_absolute_lifetime()
    {
        var now = DateTime.UtcNow;
        var token = IssueToken(now);
        // Repeatedly "use" the token just inside its sliding window, well
        // past what the absolute lifetime alone would allow.
        var current = now;
        while (current < now.Add(AuthToken.AbsoluteLifetime).AddHours(1) && token.IsValid(current))
        {
            token.ExtendExpiry(current);
            current = current.Add(AuthToken.SlidingWindow).AddMinutes(-1);
        }

        Assert.True(token.ExpiresAtUtc <= token.AbsoluteExpiresAtUtc);
    }

    [Fact]
    public void A_token_is_invalid_past_its_absolute_lifetime_even_if_continuously_used()
    {
        var now = DateTime.UtcNow;
        var token = IssueToken(now);

        Assert.False(token.IsValid(now.Add(AuthToken.AbsoluteLifetime).AddSeconds(1)));
    }

    [Fact]
    public void A_revoked_token_is_invalid_even_before_it_would_otherwise_expire()
    {
        var now = DateTime.UtcNow;
        var token = IssueToken(now);

        token.Revoke(now);

        Assert.False(token.IsValid(now));
    }

    [Fact]
    public void Revoking_an_already_revoked_token_throws()
    {
        var now = DateTime.UtcNow;
        var token = IssueToken(now);
        token.Revoke(now);

        Assert.Throws<InvalidOperationException>(() => token.Revoke(now));
    }

    [Fact]
    public void Extending_an_expired_token_throws()
    {
        var now = DateTime.UtcNow;
        var token = IssueToken(now);
        var afterExpiry = now.Add(AuthToken.SlidingWindow).AddSeconds(1);

        Assert.Throws<InvalidOperationException>(() => token.ExtendExpiry(afterExpiry));
    }
}
