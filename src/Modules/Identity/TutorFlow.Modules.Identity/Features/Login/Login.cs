using FluentValidation;
using Microsoft.AspNetCore.Identity;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Features.Login;

public static class Login
{
    public sealed record Command(string Email, string Password);

    /// <summary>
    /// Carries the access token for the endpoint layer to set as an httpOnly cookie
    /// (ADR-0023) — never serialized directly to the client. Use <see cref="SessionResponse"/>
    /// for the actual JSON body.
    /// </summary>
    public sealed record Response(
        string AccessToken,
        DateTimeOffset ExpiresAtUtc,
        Guid UserId,
        string Email,
        IReadOnlyCollection<string> Roles);

    public sealed record SessionResponse(
        Guid UserId,
        string Email,
        IReadOnlyCollection<string> Roles,
        DateTimeOffset ExpiresAtUtc);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    internal sealed class Handler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService)
    {
        private const string InvalidCredentialsMessage = "Invalid email or password.";

        // Precomputed once so an unknown-email lookup still pays the password-hashing cost,
        // closing the timing side-channel that would otherwise reveal whether an email is registered.
        private static readonly string DummyPasswordHash =
            new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), "not-a-real-password");

        public async Task<Result<Response>> HandleAsync(Command command, CancellationToken ct)
        {
            var user = await userManager.FindByEmailAsync(command.Email);
            if (user is null)
            {
                new PasswordHasher<ApplicationUser>().VerifyHashedPassword(new ApplicationUser(), DummyPasswordHash, command.Password);
                return Result.Failure<Response>(InvalidCredentialsMessage);
            }

            var signInResult = await signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);
            if (!signInResult.Succeeded)
            {
                return Result.Failure<Response>(InvalidCredentialsMessage);
            }

            var roles = (await userManager.GetRolesAsync(user)).ToArray();
            var accessToken = tokenService.CreateAccessToken(user, roles);

            return Result.Success(new Response(accessToken.Value, accessToken.ExpiresAtUtc, user.Id, user.Email!, roles));
        }
    }
}
