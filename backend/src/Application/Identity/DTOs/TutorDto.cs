using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Identity.DTOs;

// Application's own translated representation of a Tutor — never the raw
// Domain object itself (docs/adr/ADR-010-api-boundary.md: Application
// Boundary Exposure Rules).
public sealed record TutorDto(
    Guid TutorId,
    bool IsApproved,
    bool IsSuspended,
    bool IsDiscoverable,
    decimal? HourlyRate,
    string? Subject,
    string? Language,
    string? Location,
    IReadOnlyCollection<TimeSpan> OfferedDurations)
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
        tutor.OfferedDurations);
}
