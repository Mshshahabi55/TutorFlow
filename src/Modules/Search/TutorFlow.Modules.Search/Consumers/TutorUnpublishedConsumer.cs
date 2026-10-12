using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Search.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Search.Consumers;

/// <summary>De-lists a suspended tutor from search (ADR-0024) — without this, a suspended
/// tutor's document stayed <c>IsPublished = true</c> forever.</summary>
public sealed class TutorUnpublishedConsumer(SearchDbContext db)
{
    public async Task HandleAsync(TutorUnpublished message, CancellationToken ct)
    {
        var document = await db.TutorSearchDocuments.FirstOrDefaultAsync(d => d.TutorUserId == message.TutorUserId, ct);
        if (document is null)
        {
            return;
        }

        document.IsPublished = false;
        await db.SaveChangesAsync(ct);
    }
}
