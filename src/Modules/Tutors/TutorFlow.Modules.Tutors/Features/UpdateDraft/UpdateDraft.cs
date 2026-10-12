using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using GetCurrentApplicationFeature = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication;

namespace TutorFlow.Modules.Tutors.Features.UpdateDraft;

public static class UpdateDraft
{
    public sealed record Command(
        string? DisplayName = null,
        string? Headline = null,
        string? About = null,
        string? ProfilePhotoReference = null,
        string? IntroVideoReference = null,
        IReadOnlyList<TutorSpokenLanguage>? SpokenLanguages = null,
        IReadOnlyList<Guid>? SubjectIds = null,
        IReadOnlyList<Guid>? SpecialtyIds = null,
        string? CountryOfBirthCode = null,
        int? YearsOfExperience = null,
        string? ExperienceSummary = null,
        IReadOnlyList<EducationEntry>? EducationEntries = null,
        IReadOnlyList<CertificateEntry>? CertificateEntries = null,
        Money? HourlyRate = null,
        bool? AvailabilitySettingsConfigured = null,
        bool? BookingSettingsConfigured = null);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DisplayName!).MaximumLength(200).When(x => x.DisplayName is not null);
            RuleFor(x => x.Headline!).MaximumLength(200).When(x => x.Headline is not null);
            RuleFor(x => x.About!).MaximumLength(4000).When(x => x.About is not null);
            RuleFor(x => x.YearsOfExperience!.Value).GreaterThanOrEqualTo(0).When(x => x.YearsOfExperience is not null);
            RuleForEach(x => x.SubjectIds).NotEqual(Guid.Empty).When(x => x.SubjectIds is not null);
            RuleForEach(x => x.SpecialtyIds).NotEqual(Guid.Empty).When(x => x.SpecialtyIds is not null);
            RuleForEach(x => x.SpokenLanguages!)
                .Must(l => LanguageCode.IsValid(l.LanguageCode))
                .WithMessage("Each spoken language must use a valid 2-letter ISO 639-1 code.")
                .When(x => x.SpokenLanguages is not null);
            RuleFor(x => x.CountryOfBirthCode!).Length(2).When(x => x.CountryOfBirthCode is not null);
            RuleFor(x => x.HourlyRate!.AmountMinorUnits).GreaterThanOrEqualTo(0).When(x => x.HourlyRate is not null);
            RuleFor(x => x.HourlyRate!.Currency).Length(3).When(x => x.HourlyRate is not null);
        }
    }

    internal sealed class Handler(TutorsDbContext db, TimeProvider clock)
    {
        public async Task<Result<GetCurrentApplicationFeature.Response>> HandleAsync(Guid userId, Command command, CancellationToken ct)
        {
            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.UserId == userId, ct);
            if (application is null)
            {
                return Result.Failure<GetCurrentApplicationFeature.Response>("No tutor application found for this user.");
            }

            var update = new DraftUpdate(
                command.DisplayName,
                command.Headline,
                command.About,
                command.ProfilePhotoReference,
                command.IntroVideoReference,
                command.SpokenLanguages,
                command.SubjectIds,
                command.SpecialtyIds,
                command.CountryOfBirthCode,
                command.YearsOfExperience,
                command.ExperienceSummary,
                command.EducationEntries,
                command.CertificateEntries,
                command.HourlyRate,
                command.AvailabilitySettingsConfigured,
                command.BookingSettingsConfigured);

            var result = application.ApplyDraftUpdate(update, clock);
            if (result.IsFailure)
            {
                return Result.Failure<GetCurrentApplicationFeature.Response>([.. result.Errors]);
            }

            await db.SaveChangesAsync(ct);

            return Result.Success(GetCurrentApplicationFeature.Map(application));
        }
    }
}
