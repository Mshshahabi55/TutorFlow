using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Tutors.Features.GetPublicProfile;

/// <summary>
/// The public, by-slug counterpart to <c>GetCurrentApplication</c> — anonymous access, resolves
/// a stable <see cref="TutorApplication.PublicSlug"/> rather than the caller's own UserId, and
/// excludes internal-only fields (<c>RejectionReason</c>, <c>SuspensionReason</c>, raw
/// <c>AvailabilitySettingsConfigured</c>/<c>BookingSettingsConfigured</c> onboarding flags).
/// See ADR-0022, PREPLY_TUTOR_PROFILE_PARITY_SPEC.md §2.
/// </summary>
public static class GetPublicProfile
{
    public sealed record SpokenLanguageResponse(string LanguageCode, string Proficiency);

    /// <summary>Public projection of <see cref="CertificateEntry"/> — deliberately excludes
    /// <c>FileReference</c> (a server storage path/reference), <c>RejectionReason</c>, and the raw
    /// <c>VerificationStatus</c> enum; <see cref="IsVerified"/> is the only derived fact ever
    /// exposed, computed strictly from <c>VerificationStatus == Verified</c> — never true for
    /// None/PendingReview/Rejected. Mapping the domain record directly into a public,
    /// anonymous-access response, as the pre-Stage-1 implementation did, is the security gap this
    /// explicit allowlist exists to prevent from recurring as new internal fields are added.</summary>
    public sealed record PublicCertificateEntry(
        string Name,
        string IssuingOrganization,
        string? Description,
        Guid? SubjectId,
        int? StartYear,
        int? EndYear,
        bool IsVerified);

    public sealed record Response(
        Guid TutorUserId,
        string Slug,
        string DisplayName,
        string? Headline,
        string? About,
        string? ProfilePhotoReference,
        string? IntroVideoReference,
        IReadOnlyList<SpokenLanguageResponse> SpokenLanguages,
        IReadOnlyList<Guid> SubjectIds,
        IReadOnlyList<Guid> SpecialtyIds,
        string? CountryOfBirthCode,
        bool IsProfessionalTutor,
        bool IsSuperTutor,
        int? YearsOfExperience,
        string? ExperienceSummary,
        IReadOnlyList<EducationEntry> EducationEntries,
        IReadOnlyList<PublicCertificateEntry> CertificateEntries,
        int HourlyRateAmountMinorUnits,
        string HourlyRateCurrency,
        bool IsAcceptingNewStudents);

    internal sealed class Handler(TutorsDbContext db)
    {
        public async Task<Result<Response>> HandleAsync(string slug, CancellationToken ct)
        {
            var application = await db.TutorApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.PublicSlug == slug, ct);

            // Every non-Published status (Draft/Submitted/UnderReview/Approved/Rejected/
            // Suspended) is treated identically to "not found" so internal pipeline state is
            // never leaked publicly — intentionally stricter than the one Preply-observed
            // "not accepting new students" state, which IS still Published (see
            // IsAcceptingNewStudents) and therefore does NOT 404 (ADR-0022).
            if (application is null || application.Status != TutorApplicationStatus.Published)
            {
                return Result.Failure<Response>("Tutor profile not found.");
            }

            return Result.Success(new Response(
                application.UserId,
                application.PublicSlug!,
                application.DisplayName ?? "",
                application.Headline,
                application.About,
                application.ProfilePhotoReference,
                application.IntroVideoReference,
                [.. application.SpokenLanguages.Select(l => new SpokenLanguageResponse(l.LanguageCode, l.Proficiency.ToString()))],
                application.SubjectIds,
                application.SpecialtyIds,
                application.CountryOfBirthCode,
                application.IsProfessionalTutor,
                application.IsSuperTutor,
                application.YearsOfExperience,
                application.ExperienceSummary,
                application.EducationEntries,
                [.. application.CertificateEntries.Select(c => new PublicCertificateEntry(
                    c.Name,
                    c.IssuingOrganization,
                    c.Description,
                    c.SubjectId,
                    c.StartYear,
                    c.EndYear,
                    c.VerificationStatus == CertificateVerificationStatus.Verified))],
                application.HourlyRateAmountMinorUnits ?? 0,
                application.HourlyRateCurrency ?? "EUR",
                application.IsAcceptingNewStudents));
        }
    }
}
