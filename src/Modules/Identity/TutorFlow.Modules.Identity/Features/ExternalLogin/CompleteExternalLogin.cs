using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Features.ExternalLogin;

/// <summary>
/// The single place that decides what happens when a Google/Facebook sign-in presents a
/// provider identity. Deliberately takes raw, already-extracted values (never a ClaimsPrincipal
/// or HttpContext) so this is testable the same way Register.Handler/Login.Handler/
/// BecomeTutor.Handler already are — no HTTP, no real OAuth handshake, just the account
/// resolution rules themselves. The endpoint layer (IdentityModule's callback handlers) owns
/// extracting claims from the authenticated external principal and translating this Handler's
/// outcome into an HTTP redirect.
/// </summary>
public static class CompleteExternalLogin
{
    public sealed record Command(string Provider, string ProviderKey, string? Email, bool EmailVerified);

    /// <summary>
    /// Carries the access token for the endpoint layer to set as an httpOnly cookie (ADR-0023) —
    /// never serialized directly to the client. Same shape as Register/Login/BecomeTutor's own
    /// Response records, reusing the identical session-issuance contract.
    /// </summary>
    public sealed record Response(
        Guid UserId,
        string Email,
        IReadOnlyCollection<string> Roles,
        string AccessToken,
        DateTimeOffset ExpiresAtUtc);

    public sealed record SessionResponse(
        Guid UserId,
        string Email,
        IReadOnlyCollection<string> Roles,
        DateTimeOffset ExpiresAtUtc);

    /// <summary>
    /// A closed set of outcomes, not a single <c>Result</c> — the endpoint layer must map each
    /// failure to a safe, non-leaking redirect code, and matching on an error-message substring
    /// would be fragile and could accidentally leak internal detail into a URL. Matching on the
    /// outcome's type is exhaustive and compiler-checked instead.
    /// </summary>
    public abstract record Outcome
    {
        public sealed record Success(Response Session) : Outcome;

        /// <summary>No usable, provider-verified email was presented — the only safe response is
        /// to refuse account creation entirely (approved decision #5).</summary>
        public sealed record EmailRequired : Outcome;

        /// <summary>The verified email already belongs to an existing account — never
        /// auto-linked (approved decision #4); the caller must log in to the existing account
        /// first and link from there in a later phase.</summary>
        public sealed record EmailConflict : Outcome;

        public sealed record Failed(IReadOnlyCollection<string> Errors) : Outcome;
    }

    internal sealed class Handler(UserManager<ApplicationUser> userManager, ITokenService tokenService, TimeProvider clock)
    {
        public async Task<Outcome> HandleAsync(Command command, CancellationToken ct)
        {
            var existing = await userManager.FindByLoginAsync(command.Provider, command.ProviderKey);
            if (existing is not null)
            {
                var existingRoles = (await userManager.GetRolesAsync(existing)).ToArray();
                var existingToken = tokenService.CreateAccessToken(existing, existingRoles);
                return new Outcome.Success(new Response(
                    existing.Id, existing.Email!, existingRoles, existingToken.Value, existingToken.ExpiresAtUtc));
            }

            // Not linked yet — never create an account without a provider-confirmed email.
            // Google marks this explicitly via its `email_verified` claim; Facebook's own Graph
            // API never returns an email at all unless Facebook itself already confirmed it
            // (documented Facebook Login behavior — no separate verified flag exists on their
            // side), so the endpoint layer passes EmailVerified=true for Facebook whenever an
            // email claim is present at all.
            if (string.IsNullOrWhiteSpace(command.Email) || !command.EmailVerified)
            {
                return new Outcome.EmailRequired();
            }

            var existingByEmail = await userManager.FindByEmailAsync(command.Email);
            if (existingByEmail is not null)
            {
                return new Outcome.EmailConflict();
            }

            var newUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = command.Email,
                Email = command.Email,
                EmailConfirmed = true,
                CreatedAtUtc = clock.GetUtcNow()
            };

            IdentityResult createResult;
            try
            {
                createResult = await userManager.CreateAsync(newUser);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Same race Register.Handler already guards against: two concurrent sign-ups
                // (password or OAuth) for the same email can both pass FindByEmailAsync above —
                // the database's unique index is the real guard.
                return new Outcome.EmailConflict();
            }

            if (!createResult.Succeeded)
            {
                return new Outcome.Failed(createResult.Errors.Select(e => e.Description).ToArray());
            }

            // No ambient transaction spans these UserManager calls — each commits independently.
            // Compensating cleanup (delete the just-created user) is the guard against a
            // partially-created account if either step below fails, rather than wrapping
            // UserManager's own internal SaveChanges calls in an explicit transaction.
            IdentityResult addLoginResult;
            try
            {
                addLoginResult = await userManager.AddLoginAsync(
                    newUser, new UserLoginInfo(command.Provider, command.ProviderKey, command.Provider));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Two concurrent sign-ins with the same provider identity (e.g. a double-click)
                // can both pass FindByLoginAsync above before either commits — the
                // (LoginProvider, ProviderKey) unique index is the real guard.
                await userManager.DeleteAsync(newUser);
                return new Outcome.Failed(["This sign-in is already in progress. Please try again."]);
            }

            if (!addLoginResult.Succeeded)
            {
                await userManager.DeleteAsync(newUser);
                return new Outcome.Failed(addLoginResult.Errors.Select(e => e.Description).ToArray());
            }

            // Every OAuth sign-up is a brand-new account — always Student, same as
            // Register.Handler. Never derived from any caller-supplied value (approved
            // decisions #2/#3): this Handler's Command has no field that could carry one.
            var roleResult = await userManager.AddToRoleAsync(newUser, RoleNames.Student);
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(newUser);
                return new Outcome.Failed(roleResult.Errors.Select(e => e.Description).ToArray());
            }

            var accessToken = tokenService.CreateAccessToken(newUser, [RoleNames.Student]);
            return new Outcome.Success(new Response(
                newUser.Id, newUser.Email!, [RoleNames.Student], accessToken.Value, accessToken.ExpiresAtUtc));
        }
    }
}
