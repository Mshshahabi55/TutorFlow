using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Messaging.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Messaging.Features.GetMessages;

// ADR-0030: keyset/cursor pagination on the DB-generated SequenceNumber, never offset-based —
// offsets shift and can skip or duplicate rows when new messages arrive between page requests;
// a "before this sequence number" cursor cannot. Pages are newest-first: the first page (no
// cursor) is the most recent messages, and NextBeforeCursor pages backward into older history —
// the same direction every caller gets, documented here as the one contract.
public static class GetMessages
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    public sealed record MessageDto(Guid Id, Guid SenderUserId, string Body, DateTimeOffset CreatedAtUtc, long SequenceNumber);

    // Tutor is resolved once per response, at conversation level — never duplicated per message
    // (ADR-0030 Batch 69). Null only if the Tutor's profile summary is genuinely unresolvable; the
    // caller must render a safe fallback, never an error, for that case.
    public sealed record Response(TutorDisplaySummary? Tutor, IReadOnlyList<MessageDto> Messages, long? NextBeforeCursor);

    public enum Outcome
    {
        Found,
        NotFound
    }

    internal sealed class Handler(MessagingDbContext db, ITutorProfileSummaryQuery tutorProfileQuery)
    {
        // NotFound covers both "no such conversation" and "caller is not a participant" — a
        // single externally-observable outcome (ADR-0030, a deliberate deviation from
        // Bookings.GetBooking's distinguished Forbidden/NotFound) so a conversation ID cannot be
        // enumerated by distinguishing "doesn't exist" from "exists but isn't yours."
        public async Task<(Outcome Outcome, Response? Response)> HandleAsync(
            Guid callerUserId, Guid conversationId, long? before, int? take, CancellationToken ct)
        {
            var conversation = await db.Conversations.AsNoTracking()
                .Where(c => c.Id == conversationId)
                .Select(c => new { c.StudentUserId, c.TutorUserId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null || (conversation.StudentUserId != callerUserId && conversation.TutorUserId != callerUserId))
            {
                return (Outcome.NotFound, null);
            }

            // Reused exactly as-is (ADR-0014/ADR-0030): no status filter, so a since-suspended
            // Tutor's last-known display info is still returned here — this conversation's history
            // remains readable regardless of the Tutor's current bookability state, since the
            // caller has already been confirmed a genuine participant above.
            var tutorProfile = await tutorProfileQuery.GetAsync(conversation.TutorUserId, ct);
            var tutorSummary = tutorProfile is null
                ? null
                : new TutorDisplaySummary(tutorProfile.DisplayName, tutorProfile.ProfilePhotoReference, tutorProfile.PublicSlug);

            var pageSize = take is > 0 and <= MaxPageSize ? take.Value : DefaultPageSize;

            var query = db.Messages.AsNoTracking().Where(m => m.ConversationId == conversationId);
            if (before.HasValue)
            {
                // Accepted limitation (ADR-0030, Batch 64 audit): PostgreSQL's identity/sequence
                // allocation order is not the same as transaction commit order, so a message that
                // allocated a SequenceNumber below this cursor can still commit after a caller has
                // already paged past it. This strict "<" avoids the classic offset-pagination bug
                // (rows shifting as new ones are inserted), but it does not guarantee every
                // concurrently in-flight message is seen by an already-in-progress backward
                // traversal — that message is never lost, only possibly absent from this one
                // traversal (it is still visible from a fresh first-page fetch).
                query = query.Where(m => m.SequenceNumber < before.Value);
            }

            var page = await query
                .OrderByDescending(m => m.SequenceNumber)
                .Take(pageSize)
                .Select(m => new MessageDto(m.Id, m.SenderUserId, m.Body, m.CreatedAtUtc, m.SequenceNumber))
                .ToListAsync(ct);

            var nextCursor = page.Count == pageSize ? page[^1].SequenceNumber : (long?)null;
            return (Outcome.Found, new Response(tutorSummary, page, nextCursor));
        }
    }
}
