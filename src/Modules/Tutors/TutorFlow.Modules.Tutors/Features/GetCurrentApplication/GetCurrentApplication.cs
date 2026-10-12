using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Tutors.Features.GetCurrentApplication;

public static class GetCurrentApplication
{
    public sealed record Response(
        Guid Id,
        Guid UserId,
        TutorApplicationStatus Status,
        string? PublicSlug,
        string? DisplayName,
        string? Headline,
        string? About,
        string? ProfilePhotoReference,
        string? IntroVideoReference,
        IReadOnlyList<TutorSpokenLanguage> SpokenLanguages,
        IReadOnlyList<Guid> SubjectIds,
        IReadOnlyList<Guid> SpecialtyIds,
        string? CountryOfBirthCode,
        bool IsProfessionalTutor,
        bool IsSuperTutor,
        int? YearsOfExperience,
        string? ExperienceSummary,
        IReadOnlyList<EducationEntry> EducationEntries,
        IReadOnlyList<CertificateEntry> CertificateEntries,
        int? HourlyRateAmountMinorUnits,
        string? HourlyRateCurrency,
        bool AvailabilitySettingsConfigured,
        bool BookingSettingsConfigured,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        DateTimeOffset? SubmittedAtUtc,
        DateTimeOffset? ReviewedAtUtc,
        DateTimeOffset? PublishedAtUtc,
        string? RejectionReason,
        string? SuspensionReason);

    public static Response Map(TutorApplication a) => new(
        a.Id,
        a.UserId,
        a.Status,
        a.PublicSlug,
        a.DisplayName,
        a.Headline,
        a.About,
        a.ProfilePhotoReference,
        a.IntroVideoReference,
        a.SpokenLanguages,
        a.SubjectIds,
        a.SpecialtyIds,
        a.CountryOfBirthCode,
        a.IsProfessionalTutor,
        a.IsSuperTutor,
        a.YearsOfExperience,
        a.ExperienceSummary,
        a.EducationEntries,
        a.CertificateEntries,
        a.HourlyRateAmountMinorUnits,
        a.HourlyRateCurrency,
        a.AvailabilitySettingsConfigured,
        a.BookingSettingsConfigured,
        a.CreatedAtUtc,
        a.UpdatedAtUtc,
        a.SubmittedAtUtc,
        a.ReviewedAtUtc,
        a.PublishedAtUtc,
        a.RejectionReason,
        a.SuspensionReason);

    internal sealed class Handler(TutorsDbContext db)
    {
        public async Task<Result<Response>> HandleAsync(Guid userId, CancellationToken ct)
        {
            var application = await db.TutorApplications.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId, ct);
            if (application is null)
            {
                return Result.Failure<Response>("No tutor application found for this user.");
            }

            return Result.Success(Map(application));
        }
    }
}
