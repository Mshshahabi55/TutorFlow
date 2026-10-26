using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Availability.Infrastructure;

// ADR-0014: implements the Shared query contract Bookings depends on via DI, with no project
// reference between the two modules. Never queries Bookings' own holds — see
// AvailabilityCalculator.ValidateRequestedSlot's note.
internal sealed class TutorAvailabilityQuery(AvailabilityDbContext db, TimeProvider clock) : ITutorAvailabilityQuery
{
    public async Task<bool> IsTutorBookableAsync(Guid tutorUserId, CancellationToken ct)
    {
        // Same fail-closed rule as ValidateSlotAsync below, extracted as its own narrow check for
        // callers (Messaging, ADR-0030) that only need "is this tutor currently bookable at all,"
        // not a specific slot. Deliberately no try/catch: a genuine query failure must propagate
        // as an unhandled failure, never be swallowed into a confident "false" — doing so would
        // misrepresent an infrastructure problem as a business ineligibility decision.
        var bookability = await db.TutorBookabilityRecords.AsNoTracking().FirstOrDefaultAsync(b => b.TutorUserId == tutorUserId, ct);
        return bookability is { IsBookable: true };
    }

    public async Task<SlotValidationResult> ValidateSlotAsync(Guid tutorUserId, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken ct)
    {
        // Fail-closed (ADR-0014 correction): absence of a bookability record means "not
        // bookable," never "bookable by default." This is correct and expected until ADR-0011's
        // Tutors -> Availability publish/consume pipeline is wired for a given tutor.
        if (!await IsTutorBookableAsync(tutorUserId, ct))
        {
            return new SlotValidationResult(false, "tutor not bookable");
        }

        var settings = await db.BookingSettings.AsNoTracking().FirstOrDefaultAsync(s => s.TutorUserId == tutorUserId, ct);
        if (settings is null)
        {
            return new SlotValidationResult(false, "tutor booking settings not configured");
        }

        var rules = await db.WeeklyAvailabilityRules.AsNoTracking().Where(r => r.TutorUserId == tutorUserId).ToListAsync(ct);
        var timeOff = await db.TimeOffPeriods.AsNoTracking().Where(t => t.TutorUserId == tutorUserId).ToListAsync(ct);

        var durationMinutes = (int)Math.Round((endUtc - startUtc).TotalMinutes);
        var (isValid, reason) = AvailabilityCalculator.ValidateRequestedSlot(rules, timeOff, settings, clock.GetUtcNow(), startUtc, durationMinutes);

        return new SlotValidationResult(isValid, reason);
    }
}
