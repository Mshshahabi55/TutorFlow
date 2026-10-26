using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Identity.Infrastructure;
using CompleteExternalLoginFeature = TutorFlow.Modules.Identity.Features.ExternalLogin.CompleteExternalLogin;

namespace TutorFlow.Modules.Identity.Tests;

/// <summary>
/// Exercises <see cref="CompleteExternalLoginFeature.Handler"/> directly against a real Postgres
/// instance (via <see cref="IdentityApiFixture"/>'s real DI container) — the same level this
/// codebase already tests Register.Handler/Login.Handler/BecomeTutor.Handler at. Deliberately
/// does not go through a real Google/Facebook OAuth handshake (impossible without live provider
/// credentials and a real browser) or a fake HTTP authentication handler for the happy path —
/// the Handler takes already-extracted, already-verified values, which is exactly the seam the
/// real callback endpoint hands off to, so this is a faithful, deterministic substitute for it.
/// </summary>
[Collection(IdentityHostCollection.Name)]
public class CompleteExternalLoginTests(IdentityApiFixture fixture) : IClassFixture<IdentityApiFixture>
{
    private CompleteExternalLoginFeature.Handler CreateHandler(out IServiceScope scope)
    {
        scope = fixture.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<CompleteExternalLoginFeature.Handler>();
    }

    [Fact]
    public async Task HandleAsync_NewGoogleIdentity_CreatesAnAccountWithStudentRoleOnly()
    {
        var handler = CreateHandler(out var scope);
        using (scope)
        {
            var email = $"google-new-{Guid.NewGuid():N}@example.com";

            var outcome = await handler.HandleAsync(
                new CompleteExternalLoginFeature.Command("Google", $"google-sub-{Guid.NewGuid():N}", email, EmailVerified: true),
                TestContext.Current.CancellationToken);

            var success = Assert.IsType<CompleteExternalLoginFeature.Outcome.Success>(outcome);
            Assert.Equal(email, success.Session.Email);
            Assert.Equal([RoleNames.Student], success.Session.Roles);
            Assert.NotEmpty(success.Session.AccessToken);
        }
    }

    [Fact]
    public async Task HandleAsync_NewFacebookIdentity_CreatesAnAccountWithStudentRoleOnly()
    {
        var handler = CreateHandler(out var scope);
        using (scope)
        {
            var email = $"facebook-new-{Guid.NewGuid():N}@example.com";

            var outcome = await handler.HandleAsync(
                new CompleteExternalLoginFeature.Command("Facebook", $"facebook-id-{Guid.NewGuid():N}", email, EmailVerified: true),
                TestContext.Current.CancellationToken);

            var success = Assert.IsType<CompleteExternalLoginFeature.Outcome.Success>(outcome);
            Assert.Equal(email, success.Session.Email);
            Assert.Equal([RoleNames.Student], success.Session.Roles);
        }
    }

    [Fact]
    public async Task HandleAsync_AlreadyLinkedExternalIdentity_AuthenticatesTheSameExistingAccount_NeverCreatesASecondOne()
    {
        var handler = CreateHandler(out var scope);
        using (scope)
        {
            var providerKey = $"google-sub-{Guid.NewGuid():N}";
            var email = $"google-returning-{Guid.NewGuid():N}@example.com";
            var command = new CompleteExternalLoginFeature.Command("Google", providerKey, email, EmailVerified: true);

            var first = await handler.HandleAsync(command, TestContext.Current.CancellationToken);
            var firstSuccess = Assert.IsType<CompleteExternalLoginFeature.Outcome.Success>(first);

            // The second call presents the same provider identity but, realistically, the
            // provider may or may not still send a verified email — the existing-login lookup
            // must succeed on provider+providerKey alone, regardless.
            var second = await handler.HandleAsync(
                new CompleteExternalLoginFeature.Command("Google", providerKey, Email: null, EmailVerified: false),
                TestContext.Current.CancellationToken);
            var secondSuccess = Assert.IsType<CompleteExternalLoginFeature.Outcome.Success>(second);

            Assert.Equal(firstSuccess.Session.UserId, secondSuccess.Session.UserId);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_NewIdentityWithNoEmail_IsRejected_NoAccountCreated(string? missingEmail)
    {
        var handler = CreateHandler(out var scope);
        using (scope)
        {
            var outcome = await handler.HandleAsync(
                new CompleteExternalLoginFeature.Command("Google", $"google-sub-{Guid.NewGuid():N}", missingEmail, EmailVerified: true),
                TestContext.Current.CancellationToken);

            Assert.IsType<CompleteExternalLoginFeature.Outcome.EmailRequired>(outcome);
        }
    }

    [Fact]
    public async Task HandleAsync_NewIdentityWithAnUnverifiedEmail_IsRejected_NoAccountCreated()
    {
        var handler = CreateHandler(out var scope);
        using (scope)
        {
            var email = $"unverified-{Guid.NewGuid():N}@example.com";

            var outcome = await handler.HandleAsync(
                new CompleteExternalLoginFeature.Command("Google", $"google-sub-{Guid.NewGuid():N}", email, EmailVerified: false),
                TestContext.Current.CancellationToken);

            Assert.IsType<CompleteExternalLoginFeature.Outcome.EmailRequired>(outcome);

            // Confirms no account was created despite a real, well-formed email being present —
            // only the EmailVerified flag made this fail.
            var recheck = await handler.HandleAsync(
                new CompleteExternalLoginFeature.Command("Google", $"different-sub-{Guid.NewGuid():N}", email, EmailVerified: true),
                TestContext.Current.CancellationToken);
            Assert.IsType<CompleteExternalLoginFeature.Outcome.Success>(recheck);
        }
    }

    [Fact]
    public async Task HandleAsync_NewProviderIdentityWithAnEmailAlreadyRegistered_IsRejected_NotAutoLinked()
    {
        var handler = CreateHandler(out var scope);
        using (scope)
        {
            var email = $"already-registered-{Guid.NewGuid():N}@example.com";
            var firstProviderKey = $"google-sub-{Guid.NewGuid():N}";
            var created = await handler.HandleAsync(
                new CompleteExternalLoginFeature.Command("Google", firstProviderKey, email, EmailVerified: true),
                TestContext.Current.CancellationToken);
            Assert.IsType<CompleteExternalLoginFeature.Outcome.Success>(created);

            // A different provider identity presenting the SAME email must not silently sign
            // into, or link to, the account created above (approved decision #4).
            var conflict = await handler.HandleAsync(
                new CompleteExternalLoginFeature.Command("Facebook", $"facebook-id-{Guid.NewGuid():N}", email, EmailVerified: true),
                TestContext.Current.CancellationToken);

            Assert.IsType<CompleteExternalLoginFeature.Outcome.EmailConflict>(conflict);
        }
    }

    [Fact]
    public async Task HandleAsync_ConcurrentDuplicateExternalLogin_OnlyOneAccountIsEverCreated()
    {
        // Two separate scopes, not two concurrent calls sharing one — matches how two real,
        // simultaneous HTTP requests would each get their own DI scope (and therefore their own
        // IdentityDbContext instance) in production. A single shared scoped DbContext is not
        // thread-safe for concurrent operations (EF Core throws InvalidOperationException), so
        // sharing one here would test an artifact of the test setup, not the real race this
        // Handler's compensating-cleanup code is guarding against.
        var handlerA = CreateHandler(out var scopeA);
        var handlerB = CreateHandler(out var scopeB);
        using (scopeA)
        using (scopeB)
        {
            var providerKey = $"google-sub-{Guid.NewGuid():N}";
            var email = $"race-{Guid.NewGuid():N}@example.com";
            var command = new CompleteExternalLoginFeature.Command("Google", providerKey, email, EmailVerified: true);

            var results = await Task.WhenAll(
                handlerA.HandleAsync(command, TestContext.Current.CancellationToken),
                handlerB.HandleAsync(command, TestContext.Current.CancellationToken));

            // Exactly one of the two concurrent attempts must succeed in creating the account;
            // the other must fail safely (not throw, not leave a second/partial account behind)
            // — whichever loses the race hits either the email or the provider-key unique index.
            var successCount = results.Count(r => r is CompleteExternalLoginFeature.Outcome.Success);
            Assert.Equal(1, successCount);

            // Re-querying by the same provider identity must resolve to exactly one account —
            // proves no orphaned/duplicate row survived the loser's cleanup.
            using var confirmScope = fixture.Services.CreateScope();
            var confirmHandler = confirmScope.ServiceProvider.GetRequiredService<CompleteExternalLoginFeature.Handler>();
            var confirm = await confirmHandler.HandleAsync(command, TestContext.Current.CancellationToken);
            var confirmedSuccess = Assert.IsType<CompleteExternalLoginFeature.Outcome.Success>(confirm);
            var winner = results.OfType<CompleteExternalLoginFeature.Outcome.Success>().Single();
            Assert.Equal(winner.Session.UserId, confirmedSuccess.Session.UserId);
        }
    }
}
