using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Infrastructure;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using GetCurrentApplicationFeature = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication;
using SubmitCertificateDocumentFeature = TutorFlow.Modules.Tutors.Features.SubmitCertificateDocument.SubmitCertificateDocument;

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
        string? PhoneNumber = null,
        int? YearsOfExperience = null,
        string? ExperienceSummary = null,
        IReadOnlyList<EducationEntry>? EducationEntries = null,
        IReadOnlyList<CertificateEntryDraftInput>? CertificateEntries = null,
        Money? HourlyRate = null,
        bool? AvailabilitySettingsConfigured = null,
        bool? BookingSettingsConfigured = null);

    public sealed class Validator : AbstractValidator<Command>
    {
        /// <summary>Certificate-entry cap: an engineering safety limit against unbounded growth
        /// of the owned-JSON `certificate_entries` column, not a Preply-sourced rule — no
        /// evidence establishes Preply's own cap (see
        /// docs/product/preply-reverse-engineering/evidence/Preply_Certifications).</summary>
        private const int MaxCertificateEntries = 10;

        public Validator(TimeProvider clock)
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
            // No format rule beyond length — Preply's own evidenced "(optional)" field carries no
            // observed format validation either (24-...md §2 item 7); inventing one (e.g. E.164)
            // would not be evidence-based.
            RuleFor(x => x.PhoneNumber!).MaximumLength(32).When(x => x.PhoneNumber is not null);
            RuleFor(x => x.HourlyRate!.AmountMinorUnits).GreaterThanOrEqualTo(0).When(x => x.HourlyRate is not null);
            RuleFor(x => x.HourlyRate!.Currency).Length(3).When(x => x.HourlyRate is not null);

            // Certificate entries (Stage 1 — freeform only, no catalog/upload; see
            // docs/product/preply-reverse-engineering/evidence/Preply_Certifications). SubjectId
            // is checked for a non-empty GUID only, the same bar already applied to the
            // top-level SubjectIds/SpecialtyIds above — a true existence check against the
            // Subjects module's own catalog would require crossing a module boundary this
            // codebase never crosses elsewhere (modules communicate only via integration
            // events, never shared DbContext access).
            var maxYear = clock.GetUtcNow().Year + 1;
            RuleFor(x => x.CertificateEntries!)
                .Must(entries => entries.Count <= MaxCertificateEntries)
                .WithMessage($"You can add up to {MaxCertificateEntries} certificates.")
                .When(x => x.CertificateEntries is not null);
            RuleForEach(x => x.CertificateEntries!)
                .ChildRules(entry =>
                {
                    entry.RuleFor(e => e.Name).MaximumLength(200);
                    entry.RuleFor(e => e.IssuingOrganization).MaximumLength(200);
                    entry.RuleFor(e => e.Description!).MaximumLength(2000).When(e => e.Description is not null);
                    entry.RuleFor(e => e.SubjectId!.Value).NotEqual(Guid.Empty).When(e => e.SubjectId is not null);
                    entry.RuleFor(e => e.StartYear!.Value)
                        .InclusiveBetween(1950, maxYear)
                        .When(e => e.StartYear is not null);
                    entry.RuleFor(e => e.EndYear!.Value)
                        .InclusiveBetween(1950, maxYear)
                        .When(e => e.EndYear is not null);
                    entry.RuleFor(e => e)
                        .Must(e => e.StartYear is null || e.EndYear is null || e.StartYear <= e.EndYear)
                        .WithMessage("Start year must not be after end year.")
                        .WithName("YearRange");
                })
                .When(x => x.CertificateEntries is not null);
        }
    }

    internal sealed class Handler(TutorsDbContext db, ICertificateDocumentStorage storage, TimeProvider clock, ILogger<Handler> logger)
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
                command.PhoneNumber,
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
            await SubmitCertificateDocumentFeature.CleanupOrphanedFilesAsync(application, storage, logger, ct);

            return Result.Success(GetCurrentApplicationFeature.Map(application));
        }
    }
}
