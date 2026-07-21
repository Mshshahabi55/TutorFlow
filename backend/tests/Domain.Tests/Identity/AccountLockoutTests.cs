using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.Events;

namespace TutorFlow.Domain.Tests.Identity;

// Account lockout is intrinsic Account state, shared by all four role
// subclasses — Tutor is used as the concrete instance under test, but
// nothing here is Tutor-specific (Launch Preparation, Priority 1: account
// lock handling).
public class AccountLockoutTests
{
    [Fact]
    public void A_newly_registered_account_is_not_locked()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        Assert.False(tutor.IsLocked(DateTime.UtcNow));
        Assert.Equal(0, tutor.FailedLoginAttemptCount);
    }

    [Fact]
    public void Failed_attempts_below_the_threshold_do_not_lock_the_account()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var now = DateTime.UtcNow;

        for (var i = 0; i < Account.MaxFailedLoginAttempts - 1; i++)
        {
            tutor.RecordFailedLoginAttempt(now);
        }

        Assert.False(tutor.IsLocked(now));
        Assert.Equal(Account.MaxFailedLoginAttempts - 1, tutor.FailedLoginAttemptCount);
    }

    [Fact]
    public void Reaching_the_threshold_locks_the_account_and_raises_AccountLocked()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var now = DateTime.UtcNow;

        for (var i = 0; i < Account.MaxFailedLoginAttempts; i++)
        {
            tutor.RecordFailedLoginAttempt(now);
        }

        Assert.True(tutor.IsLocked(now));
        Assert.NotNull(tutor.LockedUntilUtc);
        Assert.Contains(tutor.DomainEvents, e => e is AccountLocked locked && locked.AccountId == tutor.Id);
    }

    [Fact]
    public void A_locked_account_is_no_longer_locked_once_the_lockout_window_has_passed()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var now = DateTime.UtcNow;

        for (var i = 0; i < Account.MaxFailedLoginAttempts; i++)
        {
            tutor.RecordFailedLoginAttempt(now);
        }

        Assert.False(tutor.IsLocked(now.Add(Account.LockoutDuration).AddSeconds(1)));
    }

    [Fact]
    public void Further_failed_attempts_while_locked_do_not_extend_the_lockout()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var now = DateTime.UtcNow;

        for (var i = 0; i < Account.MaxFailedLoginAttempts; i++)
        {
            tutor.RecordFailedLoginAttempt(now);
        }

        var lockedUntil = tutor.LockedUntilUtc;
        tutor.RecordFailedLoginAttempt(now.AddMinutes(1));

        Assert.Equal(lockedUntil, tutor.LockedUntilUtc);
    }

    [Fact]
    public void A_successful_login_resets_the_failed_attempt_counter()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.RecordFailedLoginAttempt(DateTime.UtcNow);

        tutor.RecordSuccessfulLogin();

        Assert.Equal(0, tutor.FailedLoginAttemptCount);
        Assert.False(tutor.IsLocked(DateTime.UtcNow));
    }

    [Fact]
    public void Resetting_the_password_clears_any_lockout_and_raises_PasswordReset()
    {
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        var now = DateTime.UtcNow;
        for (var i = 0; i < Account.MaxFailedLoginAttempts; i++)
        {
            tutor.RecordFailedLoginAttempt(now);
        }

        tutor.ResetPassword(TestCredentials.Hash());

        Assert.False(tutor.IsLocked(now));
        Assert.Equal(0, tutor.FailedLoginAttemptCount);
        Assert.Contains(tutor.DomainEvents, e => e is PasswordReset reset && reset.AccountId == tutor.Id);
    }
}
