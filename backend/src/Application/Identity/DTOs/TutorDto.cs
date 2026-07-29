using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Identity.DTOs;

// A Subject/Level pair — Application's own translated shape for
// Domain.Identity.ValueObjects.TutorSubjectEntry (docs/adr/ADR-024, Accepted).
public sealed record TutorSubjectDto(string Subject, string? Level);

// Application's own translated representation of a Tutor — never the raw
// Domain object itself (docs/adr/ADR-010-api-boundary.md: Application
// Boundary Exposure Rules). Fields below `OfferedDurations` are additive
// (docs/adr/ADR-024, Accepted, 2026-07-28 — Tutor Profile Enrichment).
public sealed record TutorDto(
    Guid TutorId,
    bool IsApproved,
    bool IsSuspended,
    bool IsDiscoverable,
    decimal? HourlyRate,
    string? Subject,
    string? Language,
    string? Location,
    IReadOnlyCollection<TimeSpan> OfferedDurations,
    string ProfileStatus,
    string? DisplayName,
    string? Headline,
    string? Biography,
    string? Country,
    string? City,
    IReadOnlyCollection<string> OtherLanguages,
    IReadOnlyCollection<TutorSubjectDto> TutorSubjects,
    int? YearsOfExperience,
    string? Education,
    string? Certifications,
    string? TeachingMethodology,
    IReadOnlyCollection<string> LessonSpecialties,
    string? PhotoUrl,
    string? IntroVideoUrl,
    IReadOnlyCollection<string> GalleryImageUrls,
    bool TrialLessonAvailable,
    decimal? TrialLessonPrice)
{
    public static TutorDto FromDomain(Tutor tutor) => new(
        tutor.Id.Value,
        tutor.IsApproved,
        tutor.IsSuspended,
        tutor.IsDiscoverable,
        tutor.HourlyRate?.Amount,
        tutor.Subject?.Value,
        tutor.Language?.Value,
        tutor.Location?.Value,
        tutor.OfferedDurations,
        tutor.ProfileStatus.ToString(),
        tutor.DisplayName,
        tutor.Headline,
        tutor.Biography,
        tutor.Country,
        tutor.City,
        tutor.OtherLanguages,
        tutor.TutorSubjects.Select(entry => new TutorSubjectDto(entry.Subject, entry.Level)).ToList(),
        tutor.YearsOfExperience,
        tutor.Education,
        tutor.Certifications,
        tutor.TeachingMethodology,
        tutor.LessonSpecialties,
        tutor.PhotoUrl,
        tutor.IntroVideoUrl,
        tutor.GalleryImageUrls,
        tutor.TrialLessonAvailable,
        tutor.TrialLessonPrice?.Amount);
}
