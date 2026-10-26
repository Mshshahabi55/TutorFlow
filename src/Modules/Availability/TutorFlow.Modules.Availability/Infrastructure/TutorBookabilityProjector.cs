using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;

namespace TutorFlow.Modules.Availability.Infrastructure;

// The consumer-side boundary for the future Tutors publication event (ADR-0011). No event
// handler calls this yet — Wolverine/messaging is intentionally not added until a real
// cross-module integration event is implemented. This exists so the read-model update logic
// is ready for that handler to call.
public sealed class TutorBookabilityProjector(AvailabilityDbContext db, TimeProvider clock)
{
    // A44 replay-safety fix: occurredAtUtc is the publishing event's own timestamp
    // (TutorPublished/TutorUnpublished.OccurredAtUtc) — passed through to TutorBookability so a
    // stale/out-of-order redelivery can be detected and ignored rather than blindly overwriting
    // a legitimately newer state (see TutorBookability.TryApply).
    public async Task ApplyBookabilityChangedAsync(Guid tutorUserId, bool isBookable, DateTimeOffset occurredAtUtc, CancellationToken ct)
    {
        var existing = await db.TutorBookabilityRecords.FirstOrDefaultAsync(b => b.TutorUserId == tutorUserId, ct);

        if (existing is null)
        {
            db.TutorBookabilityRecords.Add(TutorBookability.Create(tutorUserId, isBookable, clock, occurredAtUtc));
        }
        else
        {
            existing.TryApply(isBookable, occurredAtUtc, clock);
        }

        await db.SaveChangesAsync(ct);
    }
}
