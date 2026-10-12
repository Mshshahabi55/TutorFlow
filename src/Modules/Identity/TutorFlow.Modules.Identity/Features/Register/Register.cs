using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Features.Register;

public static class Register
{
    public sealed record Command(string Email, string Password);

    /// <summary>
    /// Carries the access token for the endpoint layer to set as an httpOnly cookie
    /// (ADR-0023) — never serialized directly to the client. Use <see cref="SessionResponse"/>
    /// for the actual JSON body. Signup auto-creates a session, same as login.
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

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        }
    }

    internal sealed class Handler(UserManager<ApplicationUser> userManager, ITokenService tokenService, TimeProvider clock)
    {
        public async Task<Result<Response>> HandleAsync(Command command, CancellationToken ct)
        {
            var existing = await userManager.FindByEmailAsync(command.Email);
            if (existing is not null)
            {
                return Result.Failure<Response>("A user with this email already exists.");
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = command.Email,
                Email = command.Email,
                CreatedAtUtc = clock.GetUtcNow()
            };

            IdentityResult createResult;
            try
            {
                createResult = await userManager.CreateAsync(user, command.Password);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Two concurrent registrations for the same email can both pass the FindByEmailAsync
                // check above; the database's unique index is the real guard against that race.
                return Result.Failure<Response>("A user with this email already exists.");
            }

            if (!createResult.Succeeded)
            {
                return Result.Failure<Response>(createResult.Errors.Select(e => e.Description).ToArray());
            }

            var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Student);
            if (!roleResult.Succeeded)
            {
                return Result.Failure<Response>(roleResult.Errors.Select(e => e.Description).ToArray());
            }

            var accessToken = tokenService.CreateAccessToken(user, [RoleNames.Student]);

            return Result.Success(new Response(user.Id, user.Email!, [RoleNames.Student], accessToken.Value, accessToken.ExpiresAtUtc));
        }
    }
}
