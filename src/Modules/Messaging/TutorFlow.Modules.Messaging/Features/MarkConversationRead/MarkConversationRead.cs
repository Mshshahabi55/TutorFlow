using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Messaging.Persistence;

namespace TutorFlow.Modules.Messaging.Features.MarkConversationRead;

public static class MarkConversationRead
{
    public enum Outcome
    {
        Updated,
        NotFound
    }

    internal sealed class Handler(MessagingDbContext db, TimeProvider clock)
    {
        // NotFound covers both "no such conversation" and "caller is not a participant" — same
        // uniform-404 rule as GetMessages/SendMessage (ADR-0030).
        public async Task<Outcome> HandleAsync(Guid callerUserId, Guid conversationId, CancellationToken ct)
        {
            var conversation = await db.Conversations
                .Where(c => c.Id == conversationId)
                .Select(c => new { c.StudentUserId, c.TutorUserId })
                .FirstOrDefaultAsync(ct);

            if (conversation is null)
            {
                return Outcome.NotFound;
            }

            var now = clock.GetUtcNow();

            // Exactly one of these two guarded, single-column updates ever runs — the caller can
            // only ever be the Student or the Tutor, never both, and the WHERE clause on the
            // specific side the caller belongs to means the *other* participant's read-state
            // column is structurally unreachable from this call, not merely left alone by
            // convention.
            int affected;
            if (conversation.StudentUserId == callerUserId)
            {
                affected = await db.Conversations
                    .Where(c => c.Id == conversationId && c.StudentUserId == callerUserId)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.StudentLastReadAtUtc, now), ct);
            }
            else if (conversation.TutorUserId == callerUserId)
            {
                affected = await db.Conversations
                    .Where(c => c.Id == conversationId && c.TutorUserId == callerUserId)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.TutorLastReadAtUtc, now), ct);
            }
            else
            {
                return Outcome.NotFound;
            }

            return affected == 1 ? Outcome.Updated : Outcome.NotFound;
        }
    }
}
