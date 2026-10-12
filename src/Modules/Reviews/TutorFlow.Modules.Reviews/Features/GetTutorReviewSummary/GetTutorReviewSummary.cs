using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Reviews.Persistence;

namespace TutorFlow.Modules.Reviews.Features.GetTutorReviewSummary;

// Public, anonymous — matches the shape TutorReviews.tsx's own pre-existing comment already
// documented (ADR-0022's deferred-Reviews note), with one deliberate adjustment: no `authorName`
// field. Preply's own real profile shows a *mix* of named and anonymous reviewers (OBSERVED,
// 01-real-user-observations.md), but nothing in this project's evidence establishes how that
// choice is made, and TutorFlow has no mechanism anywhere to resolve a student's display name
// for public exposure (Students module exposes no public profile at all). Rather than fabricate
// a name or build new, unrelated Students public-identity surface, every review is anonymous in
// this slice — an honest simplification, not an invented Preply behavior.
public static class GetTutorReviewSummary
{
    public sealed record ReviewEntry(int Rating, string? Comment, DateTimeOffset CreatedAtUtc);

    public sealed record Response(double OverallRating, int ReviewCount, SubRatings? SubRatings, IReadOnlyList<ReviewEntry> Reviews);

    // DOCUMENTED (help.preply.com/articles/11196386): each dimension is shown publicly as "an
    // average star rating and indicates how many anonymous student reviews it is based on" — an
    // aggregate, never the individual submissions, and each dimension's count can differ from the
    // others (ratings are independently optional per submission). `null` means that specific
    // dimension has zero submissions yet — never fabricated as 0 or omitted silently.
    public sealed record SubRatingDimension(double? Average, int Count);

    public sealed record SubRatings(SubRatingDimension Clarity, SubRatingDimension Progress, SubRatingDimension Preparation, SubRatingDimension Reassurance);

    internal sealed class Handler(ReviewsDbContext db)
    {
        public async Task<Response> HandleAsync(Guid tutorUserId, CancellationToken ct)
        {
            var reviews = await db.Reviews
                .AsNoTracking()
                .Where(r => r.TutorUserId == tutorUserId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => new ReviewEntry(r.Rating, r.Comment, r.CreatedAtUtc))
                .ToListAsync(ct);

            var overallRating = reviews.Count == 0 ? 0 : reviews.Average(r => r.Rating);

            var subcategoryRatings = await db.SubcategoryRatings
                .AsNoTracking()
                .Where(r => r.TutorUserId == tutorUserId)
                .Select(r => new { r.Clarity, r.Progress, r.Preparation, r.Reassurance })
                .ToListAsync(ct);

            var subRatings = subcategoryRatings.Count == 0
                ? null
                : new SubRatings(
                    Dimension(subcategoryRatings.Select(r => r.Clarity)),
                    Dimension(subcategoryRatings.Select(r => r.Progress)),
                    Dimension(subcategoryRatings.Select(r => r.Preparation)),
                    Dimension(subcategoryRatings.Select(r => r.Reassurance)));

            return new Response(overallRating, reviews.Count, subRatings, reviews);
        }

        private static SubRatingDimension Dimension(IEnumerable<int?> values)
        {
            var rated = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            return rated.Count == 0 ? new SubRatingDimension(null, 0) : new SubRatingDimension(rated.Average(), rated.Count);
        }
    }
}
