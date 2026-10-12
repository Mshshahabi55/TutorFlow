using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Search.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Search.Consumers;

/// <summary>
/// Keeps <c>TutorSearchDocument.HasUpcomingAvailability</c> current (ADR-0024, closing the gap
/// where it was permanently false). No-ops if the document doesn't exist yet — ordering between
/// this event and <c>TutorPublished</c> across two independent consuming modules isn't
/// guaranteed, and <c>TutorPublished</c> will set the correct baseline regardless.
/// </summary>
public sealed class TutorAvailabilitySignalChangedConsumer(SearchDbContext db)
{
    public async Task HandleAsync(TutorAvailabilitySignalChanged message, CancellationToken ct)
    {
        var document = await db.TutorSearchDocuments.FirstOrDefaultAsync(d => d.TutorUserId == message.TutorUserId, ct);
        if (document is null)
        {
            return;
        }

        document.HasUpcomingAvailability = message.HasUpcomingAvailability;
        await db.SaveChangesAsync(ct);
    }
}
