using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

// ADR-024 (Accepted, 2026-07-28): additive alongside the existing single
// Subject field (Tutor.Subject) — a Tutor may declare several of these.
// Both Subject and Level are free text (DOMAIN_MODEL.md Open Question 10
// remains open for the taxonomy question; this does not presume an answer).
public sealed class TutorSubjectEntry : ValueObject
{
    private TutorSubjectEntry(string subject, string? level)
    {
        Subject = subject;
        Level = level;
    }

    public string Subject { get; }

    public string? Level { get; }

    public static TutorSubjectEntry Of(string subject, string? level)
    {
        var trimmedSubject = Guard.Against.NullOrWhiteSpace(subject, nameof(subject)).Trim();
        if (trimmedSubject.Length > TutorProfileLimits.MaxSubjectEntryFieldLength)
        {
            throw new ArgumentException(
                $"Subject cannot exceed {TutorProfileLimits.MaxSubjectEntryFieldLength} characters.", nameof(subject));
        }

        var trimmedLevel = string.IsNullOrWhiteSpace(level) ? null : level.Trim();
        if (trimmedLevel is not null && trimmedLevel.Length > TutorProfileLimits.MaxSubjectEntryFieldLength)
        {
            throw new ArgumentException(
                $"Level cannot exceed {TutorProfileLimits.MaxSubjectEntryFieldLength} characters.", nameof(level));
        }

        return new TutorSubjectEntry(trimmedSubject, trimmedLevel);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Subject;
        yield return Level;
    }
}
