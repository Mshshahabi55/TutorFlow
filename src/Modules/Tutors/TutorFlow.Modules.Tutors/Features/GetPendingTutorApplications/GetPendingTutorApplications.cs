using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Tutors.Features.GetPendingTutorApplications;

/// <summary>
/// Admin review queue. Minimal viable surface, not a general-purpose applications browser —
/// nothing in the available evidence (`16-product-decisions-and-open-questions.md`,
/// `18-screenshot-evidence.md`) describes Preply's own internal admin UI (it isn't publicly
/// observable), so this list's shape is an engineering proposal, not a reverse-engineered one.
/// Defaults to the real queue (Submitted + UnderReview); an explicit <see cref="Query.Status"/>
/// lets an admin browse any single status for history/audit purposes.
/// </summary>
public static class GetPendingTutorApplications
{
    public sealed record Query(TutorApplicationStatus? Status = null, int Page = 1, int PageSize = 20);

    public sealed record ApplicationSummary(
        Guid Id,
        Guid UserId,
        TutorApplicationStatus Status,
        string? DisplayName,
        string? Headline,
        string? ProfilePhotoReference,
        IReadOnlyList<Guid> SubjectIds,
        DateTimeOffset? SubmittedAtUtc);

    public sealed record Response(IReadOnlyList<ApplicationSummary> Items, int Page, int PageSize, int TotalCount);

    public sealed class Validator : AbstractValidator<Query>
    {
        public const int MaxPageSize = 50;

        public Validator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
            RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
        }
    }

    internal sealed class Handler(TutorsDbContext db)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken ct)
        {
            IQueryable<TutorApplication> applications = db.TutorApplications.AsNoTracking();

            applications = query.Status is { } status
                ? applications.Where(a => a.Status == status)
                : applications.Where(a => a.Status == TutorApplicationStatus.Submitted || a.Status == TutorApplicationStatus.UnderReview);

            // Oldest-first — the fairest default queue order for a review backlog. Not a
            // Preply-sourced detail (admin ordering isn't observable); a deliberate engineering
            // choice, flagged as such rather than implied to be benchmarked.
            applications = applications.OrderBy(a => a.SubmittedAtUtc ?? a.CreatedAtUtc).ThenBy(a => a.Id);

            var totalCount = await applications.CountAsync(ct);

            var items = await applications
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(a => new ApplicationSummary(
                    a.Id, a.UserId, a.Status, a.DisplayName, a.Headline, a.ProfilePhotoReference, a.SubjectIds, a.SubmittedAtUtc))
                .ToListAsync(ct);

            return new Response(items, query.Page, query.PageSize, totalCount);
        }
    }
}
