using FluentValidation;
using Microsoft.AspNetCore.Identity;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Features.Profile;

/// <summary>Sets the caller's own First/Last name (Identity-owned — see <see cref="ApplicationUser"/>'s
/// doc comment). Neither field is required by this handler itself: no evidence source ever
/// observed Preply enforce a required-field rule on its About stage
/// (docs/product/preply-reverse-engineering/24-about-stage-live-evidence-2026-10-18.md §1), so no
/// requiredness is invented here either.</summary>
public static class UpdateProfile
{
    public sealed record Command(string? FirstName = null, string? LastName = null);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FirstName!).MaximumLength(200).When(x => x.FirstName is not null);
            RuleFor(x => x.LastName!).MaximumLength(200).When(x => x.LastName is not null);
        }
    }

    internal sealed class Handler(UserManager<ApplicationUser> userManager)
    {
        public async Task<Result<GetProfile.Response>> HandleAsync(Guid userId, Command command, CancellationToken ct)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Result.Failure<GetProfile.Response>("Unable to resolve the current user.");
            }

            if (command.FirstName is not null)
            {
                user.FirstName = command.FirstName;
            }

            if (command.LastName is not null)
            {
                user.LastName = command.LastName;
            }

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return Result.Failure<GetProfile.Response>(updateResult.Errors.Select(e => e.Description).ToArray());
            }

            return Result.Success(new GetProfile.Response(user.FirstName, user.LastName, user.Email!));
        }
    }
}
