using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Messaging.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Messaging.Features.ListConversations;

public static class ListConversations
{
    public sealed record ConversationSummary(
        Guid Id, Guid StudentUserId, Guid TutorUserId, DateTimeOffset CreatedAtUtc,
        DateTimeOffset? LastMessageAtUtc, DateTimeOffset? MyLastReadAtUtc,
        TutorDisplaySummary? Tutor);

    internal sealed class Handler(MessagingDbContext db, ITutorProfileSummaryQuery tutorProfileQuery)
    {
        // Temporary V2 cap (ADR-0030 Batch 69), not full conversation-list pagination — bounds the
        // per-row ITutorProfileSummaryQuery lookup below to a small, known constant regardless of
        // how many conversations a user accumulates historically, since no batch variant of that
        // query exists and adding one is explicitly out of scope for this change.
        public const int MaxConversations = 50;

        // Filtered by the authenticated caller's own ID on the server (never a client-supplied
        // "whose conversations to list" parameter) — a caller only ever sees conversations where
        // they are the Student or the Tutor.
        public async Task<IReadOnlyList<ConversationSummary>> HandleAsync(Guid callerUserId, CancellationToken ct)
        {
            // The cap is applied here, before ToListAsync, so EF Core translates it into the
            // query's own LIMIT clause — the database only ever returns at most MaxConversations
            // rows, never more rows fetched and trimmed in memory afterward.
            var rows = await db.Conversations.AsNoTracking()
                .Where(c => c.StudentUserId == callerUserId || c.TutorUserId == callerUserId)
                // Deterministic tie-breaker: LastMessageAtUtc ties (including the common
                // "never messaged yet" null case) fall back to CreatedAtUtc, both descending, so
                // the ordering is stable across repeated calls rather than arbitrary.
                .OrderByDescending(c => c.LastMessageAtUtc)
                .ThenByDescending(c => c.CreatedAtUtc)
                .Take(MaxConversations)
                .Select(c => new
                {
                    c.Id,
                    c.StudentUserId,
                    c.TutorUserId,
                    c.CreatedAtUtc,
                    c.LastMessageAtUtc,
                    MyLastReadAtUtc = c.StudentUserId == callerUserId ? c.StudentLastReadAtUtc : c.TutorLastReadAtUtc
                })
                .ToListAsync(ct);

            // ITutorProfileSummaryQuery is a cross-module service call, not translatable into the
            // query above — resolved per row afterward, bounded by the cap applied in the database
            // query (at most MaxConversations calls, never unbounded). Reused exactly as-is
            // (ADR-0014/ADR-0030): no status filter, so a since-suspended Tutor's last-known
            // display info is still returned — conversation history is never hidden by a later
            // publication change. A null result (profile genuinely unresolvable) maps to a null
            // Tutor summary; the frontend is expected to render a safe fallback.
            var summaries = new List<ConversationSummary>(rows.Count);
            foreach (var row in rows)
            {
                var tutorProfile = await tutorProfileQuery.GetAsync(row.TutorUserId, ct);
                summaries.Add(new ConversationSummary(
                    row.Id,
                    row.StudentUserId,
                    row.TutorUserId,
                    row.CreatedAtUtc,
                    row.LastMessageAtUtc,
                    row.MyLastReadAtUtc,
                    tutorProfile is null ? null : new TutorDisplaySummary(tutorProfile.DisplayName, tutorProfile.ProfilePhotoReference, tutorProfile.PublicSlug)));
            }

            return summaries;
        }
    }
}
