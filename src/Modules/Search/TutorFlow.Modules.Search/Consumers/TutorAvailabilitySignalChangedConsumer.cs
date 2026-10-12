using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Search.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Search.Consumers;

/// <summary>
/// Keeps <c>TutorSearchDocument.HasUpcomingAvailability</c> current (ADR-0024, closing the gap
/// where it was permanently false). No-ops if the document doesn't exist yet — ordering between
/// this event and <c>TutorPublished</c> across two independent consuming modules isn't
/// guaranteed, and <c>TutorPublished</c> will set the correct baseline regardless.
///
/// Two <c>TutorAvailabilitySignalChanged</c> events for the *same* tutor also aren't guaranteed
/// in-order delivery (e.g. one published from <c>TutorPublished</c>'s downstream consumer,
/// another from a self-service rules/settings change moments later) — a plain unconditional
/// overwrite would let an older event processed after a newer one silently revert the field to a
/// stale value. The <c>WHERE</c> guard below makes "only apply if same-or-newer" atomic at the
/// database level: Postgres re-evaluates the predicate against the committed row under a
/// row-level lock at the moment each `UPDATE` actually executes, not against a stale in-memory
/// read from before the lock — so this is safe under genuinely concurrent delivery, not only
/// sequential out-of-order delivery. Equal timestamps are accepted (not rejected), so redelivery
/// of the exact same event (e.g. an at-least-once retry) is idempotent.
/// </summary>
public sealed class TutorAvailabilitySignalChangedConsumer(SearchDbContext db)
{
    public Task HandleAsync(TutorAvailabilitySignalChanged message, CancellationToken ct) =>
        db.TutorSearchDocuments
            .Where(d => d.TutorUserId == message.TutorUserId
                && (d.AvailabilitySignalOccurredAtUtc == null || d.AvailabilitySignalOccurredAtUtc <= message.OccurredAtUtc))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.HasUpcomingAvailability, message.HasUpcomingAvailability)
                .SetProperty(d => d.AvailabilitySignalOccurredAtUtc, message.OccurredAtUtc), ct);
}
