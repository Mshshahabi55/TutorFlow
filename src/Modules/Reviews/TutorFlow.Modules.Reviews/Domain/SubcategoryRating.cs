namespace TutorFlow.Modules.Reviews.Domain;

// Shared by SubcategoryRating.Create and the eligibility check (GetSubcategoryRatingEligibility)
// so the "once per month" window is computed identically in exactly one place.
internal static class SubcategoryRatingMonthKey
{
    public static string For(DateTimeOffset value) => $"{value.Year:D4}-{value.Month:D2}";
}

// DOCUMENTED (help.preply.com/articles/11196386): a genuinely separate submission flow from the
// written Review — "once per month, after a completed lesson, learners see a short, optional
// prompt" to rate Clarity/Progress/Preparation/Reassurance, each independently optional ("rate
// all, some, or none"), 1-5 scale, and NOT student-editable afterward ("can only be reviewed if a
// student has violated Preply's policies" — a Support-mediated dispute, not self-service editing).
//
// Each eligible submission is a NEW, immutable row — not an upsert onto Review — because the
// public display is a per-dimension AVERAGE across however many submissions exist ("indicates how
// many anonymous student reviews it is based on"); a single mutable value-per-dimension would
// silently misrepresent that count as always 1. Base eligibility (>=1 completed lesson with this
// tutor) is intentionally looser than the written Review's 3-lesson threshold — DOCUMENTED as a
// distinct rule, not assumed identical.
public sealed class SubcategoryRating
{
    private SubcategoryRating()
    {
    }

    public Guid Id { get; private init; }

    public Guid StudentUserId { get; private init; }

    public Guid TutorUserId { get; private init; }

    public int? Clarity { get; private init; }

    public int? Progress { get; private init; }

    public int? Preparation { get; private init; }

    public int? Reassurance { get; private init; }

    // "Once per month" — DOCUMENTED, but the article doesn't specify calendar-month vs. rolling-
    // window semantics (NOT VERIFIED). Calendar month (UTC) is used as the most literal,
    // defensible reading — an engineering interpretation, not an observed exact mechanism —
    // enforced via a DB-unique index on (StudentUserId, TutorUserId, SubmissionMonthKey).
    public string SubmissionMonthKey { get; private init; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private init; }

    public static SubcategoryRating Create(
        Guid studentUserId,
        Guid tutorUserId,
        int? clarity,
        int? progress,
        int? preparation,
        int? reassurance,
        TimeProvider clock)
    {
        var now = clock.GetUtcNow();
        return new SubcategoryRating
        {
            Id = Guid.NewGuid(),
            StudentUserId = studentUserId,
            TutorUserId = tutorUserId,
            Clarity = clarity,
            Progress = progress,
            Preparation = preparation,
            Reassurance = reassurance,
            SubmissionMonthKey = SubcategoryRatingMonthKey.For(now),
            CreatedAtUtc = now
        };
    }
}
