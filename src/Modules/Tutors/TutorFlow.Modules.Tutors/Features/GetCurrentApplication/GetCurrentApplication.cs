using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Tutors.Features.GetCurrentApplication;

public static class GetCurrentApplication
{
    /// <summary>Audit finding F3 — the owner's (and, via <c>GetTutorApplicationForAdmin</c>, the
    /// admin's) own projection of <see cref="CertificateEntry"/>. Deliberately excludes the raw
    /// <c>FileReference</c> (an internal, server-generated storage filename that serves no purpose
    /// client-side — both the tutor's own and the admin's "view document" actions already address
    /// the document by <see cref="Id"/> through the dedicated streaming endpoint, never by this
    /// filename) in favor of the one fact a client actually needs: whether a document is currently
    /// attached. <c>VerificationStatus</c>/<c>RejectionReason</c> ARE still included here — unlike
    /// <see cref="GetPublicProfile.GetPublicProfile.PublicCertificateEntry"/>, this is an
    /// authenticated, owner/admin-only view that legitimately needs the full review state, not the
    /// public allowlist.</summary>
    public sealed record CertificateEntryResponse(
        Guid Id,
        string Name,
        string IssuingOrganization,
        string? Description,
        Guid? SubjectId,
        int? StartYear,
        int? EndYear,
        bool HasDocument,
        CertificateVerificationStatus VerificationStatus,
        string? RejectionReason)
    {
        public static CertificateEntryResponse FromDomain(CertificateEntry c) => new(
            c.Id, c.Name, c.IssuingOrganization, c.Description, c.SubjectId, c.StartYear, c.EndYear,
            c.FileReference is not null, c.VerificationStatus, c.RejectionReason);
    }

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
        string? PhoneNumber,
        bool IsProfessionalTutor,
        bool IsSuperTutor,
        int? YearsOfExperience,
        string? ExperienceSummary,
        IReadOnlyList<EducationEntry> EducationEntries,
        IReadOnlyList<CertificateEntryResponse> CertificateEntries,
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
        a.PhoneNumber,
        a.IsProfessionalTutor,
        a.IsSuperTutor,
        a.YearsOfExperience,
        a.ExperienceSummary,
        a.EducationEntries,
        [.. a.CertificateEntries.Select(CertificateEntryResponse.FromDomain)],
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
