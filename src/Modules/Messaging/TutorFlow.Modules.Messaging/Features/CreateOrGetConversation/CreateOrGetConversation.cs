using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Messaging.Domain;
using TutorFlow.Modules.Messaging.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Messaging.Features.CreateOrGetConversation;

// ADR-0030: a Student may initiate a conversation with any currently-bookable (published) Tutor —
// no Booking required (DOCUMENTED Preply behavior: pre-trial contact is possible). Idempotent via
// a DB-unique (StudentUserId, TutorUserId) index, not an in-memory guard — a retried call lands
// on the same row.
public static class CreateOrGetConversation
{
    public sealed record Command(Guid TutorUserId);

    public sealed record Response(Guid Id, Guid StudentUserId, Guid TutorUserId, DateTimeOffset CreatedAtUtc);

    public enum Outcome
    {
        Created,
        Existing,
        TutorNotEligible
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TutorUserId).NotEmpty();
        }
    }

    internal sealed class Handler(MessagingDbContext db, TimeProvider clock, ITutorAvailabilityQuery tutorAvailabilityQuery)
    {
        public async Task<(Outcome Outcome, Response? Response)> HandleAsync(Guid studentUserId, Guid tutorUserId, CancellationToken ct)
        {
            // An existing conversation is always retrievable regardless of the tutor's current
            // eligibility — eligibility gates creating a *new* relationship, it does not retract
            // access to one that already exists (a tutor later becoming unpublished must not
            // silently hide the student's own prior conversation history).
            var existing = await ReadAsync(studentUserId, tutorUserId, ct);
            if (existing is not null)
            {
                return (Outcome.Existing, existing);
            }

            // Fail-closed (ADR-0014/ADR-0030): a query failure here must propagate as an
            // unhandled failure, never be silently treated as "not eligible" — see
            // ITutorAvailabilityQuery.IsTutorBookableAsync's own doc comment.
            if (!await tutorAvailabilityQuery.IsTutorBookableAsync(tutorUserId, ct))
            {
                return (Outcome.TutorNotEligible, null);
            }

            db.Conversations.Add(Conversation.Create(studentUserId, tutorUserId, clock));

            try
            {
                await db.SaveChangesAsync(ct);
                return (Outcome.Created, await ReadAsync(studentUserId, tutorUserId, ct));
            }
            catch (Exception ex) when (ex is not OperationCanceledException && IsUniqueViolation(ex))
            {
                // Lost the create race — a concurrent request already created this pair's
                // conversation between the read above and this insert. Re-query and return the
                // winner's row so both callers converge on the same conversation identity, the
                // same insert-then-reread-on-conflict idiom SubmitReview already established.
                return (Outcome.Existing, await ReadAsync(studentUserId, tutorUserId, ct));
            }
        }

        private async Task<Response?> ReadAsync(Guid studentUserId, Guid tutorUserId, CancellationToken ct)
        {
            return await db.Conversations.AsNoTracking()
                .Where(c => c.StudentUserId == studentUserId && c.TutorUserId == tutorUserId)
                .Select(c => new Response(c.Id, c.StudentUserId, c.TutorUserId, c.CreatedAtUtc))
                .FirstOrDefaultAsync(ct);
        }

        private static bool IsUniqueViolation(Exception ex) =>
            ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } };
    }
}
