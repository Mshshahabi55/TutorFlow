using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Lessons.Domain;
using TutorFlow.Modules.Lessons.Persistence;

namespace TutorFlow.Modules.Lessons.Features.CreateLessonFromBooking;

// ADR-0016 §1/§3: event-driven, never synchronous with Booking confirmation; duplicate-creation
// prevention is Lessons' own responsibility, not Bookings'. Public, not internal — this is the
// direct dependency of BookingConfirmedConsumer, a Wolverine-discovered consumer, and an
// internal concrete type can't be referenced from Wolverine's generated construction code (same
// reason ConfirmBooking.Handler is public).
public static class CreateLessonFromBooking
{
    public enum Outcome
    {
        Created,
        AlreadyExists
    }

    public sealed class Handler(LessonsDbContext db, TimeProvider clock)
    {
        public async Task<Outcome> HandleAsync(
            Guid bookingId,
            Guid studentUserId,
            Guid tutorUserId,
            DateTimeOffset startUtc,
            DateTimeOffset endUtc,
            CancellationToken ct)
        {
            // Pre-check, same shape as AddFavorite/CreatePaymentIntent's existing-row check —
            // the real guard against concurrent duplicate delivery is the DB unique index
            // below, not this check, which only avoids an unnecessary insert attempt on the
            // common (non-concurrent) at-least-once-redelivery case.
            var alreadyExists = await db.Lessons.AsNoTracking().AnyAsync(l => l.BookingId == bookingId, ct);
            if (alreadyExists)
            {
                return Outcome.AlreadyExists;
            }

            db.Lessons.Add(Lesson.CreateScheduled(bookingId, studentUserId, tutorUserId, startUtc, endUtc, clock));

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Two concurrent deliveries of the same BookingConfirmed event (Wolverine's
                // at-least-once guarantee) can both pass the check above; the database's unique
                // index on BookingId is the real guard — the loser's insert is rejected and
                // treated as idempotent success, never retried, never surfaced as an error.
                return Outcome.AlreadyExists;
            }

            return Outcome.Created;
        }
    }
}
