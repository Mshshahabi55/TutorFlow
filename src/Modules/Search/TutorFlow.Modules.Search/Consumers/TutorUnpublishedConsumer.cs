using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Search.Persistence;
using TutorFlow.Shared;
using Wolverine.Attributes;

namespace TutorFlow.Modules.Search.Consumers;

/// <summary>De-lists a suspended tutor from search (ADR-0024) — without this, a suspended
/// tutor's document stayed <c>IsPublished = true</c> forever. Explicitly marked for Wolverine
/// discovery (F6 — see Program.cs's DisableConventionalDiscovery comment).</summary>
/// <remarks>
/// A44: <c>AutoApplyTransactions()</c> (Program.cs) groups every handler of the same message
/// type into one chain — <c>TutorUnpublished</c> also has an
/// <c>Availability.TutorUnpublishedConsumer</c> subscriber backed by a different DbContext,
/// making the pair ambiguous without an explicit designation per handler. Search was never one
/// of A44's four target publisher modules, and this handler is a simple idempotent upsert with
/// no outgoing publish of its own — opting out preserves its current behavior unchanged.
/// </remarks>
[WolverineHandler]
[NonTransactional]
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
