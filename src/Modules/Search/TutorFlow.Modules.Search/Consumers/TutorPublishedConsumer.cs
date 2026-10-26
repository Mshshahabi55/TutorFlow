using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Search.Domain;
using TutorFlow.Modules.Search.Persistence;
using TutorFlow.Shared;
using Wolverine.Attributes;

namespace TutorFlow.Modules.Search.Consumers;

/// <summary>
/// Explicitly marked for Wolverine discovery (F6 — see Program.cs's
/// DisableConventionalDiscovery comment) rather than relying on the default "type name ends with
/// Handler/Consumer" naming convention. Constructor-injected <c>SearchDbContext</c>, matching
/// every other Handler class in this codebase. <c>SearchDbContext</c> is explicitly allow-listed
/// for service location in <c>Program.cs</c> (<c>AlwaysUseServiceLocationFor</c>) —
/// <c>AddDbContext</c> registers <c>DbContextOptions&lt;T&gt;</c> via an opaque lambda factory
/// that Wolverine 6's codegen otherwise refuses to resolve (<c>InvalidServiceLocationException</c>
/// at startup). Upserts the one row <c>TutorPublished</c> describes; never touches any other
/// tutor's document.
/// </summary>
/// <remarks>
/// A44: <c>AutoApplyTransactions()</c> (Program.cs) groups every handler of the same message
/// type into one chain — <c>TutorPublished</c> also has an
/// <c>Availability.TutorPublishedConsumer</c> subscriber backed by a different DbContext, making
/// the pair ambiguous without an explicit designation per handler. Search was never one of A44's
/// four target publisher modules, and this handler is a simple idempotent upsert with no
/// outgoing publish of its own — opting out preserves its current behavior unchanged.
/// </remarks>
[WolverineHandler]
[NonTransactional]
public sealed class TutorPublishedConsumer(SearchDbContext db)
{
    public async Task HandleAsync(TutorPublished message, CancellationToken ct)
    {
        var document = await db.TutorSearchDocuments.FirstOrDefaultAsync(d => d.TutorUserId == message.TutorUserId, ct);
        if (document is null)
        {
            document = new TutorSearchDocument { TutorUserId = message.TutorUserId };
            db.TutorSearchDocuments.Add(document);
        }

        document.Slug = message.Slug;
        document.DisplayName = message.DisplayName;
        document.Headline = message.Headline;
        document.PhotoUrl = message.PhotoUrl;
        document.SubjectIds = [.. message.SubjectIds];
        document.SpecialtyIds = [.. message.SpecialtyIds];
        document.SpokenLanguageCodes = [.. message.SpokenLanguages.Select(l => l.LanguageCode)];
        document.NativeLanguageCodes = [.. message.SpokenLanguages.Where(l => l.Proficiency == "Native").Select(l => l.LanguageCode)];
        document.CountryOfBirthCode = message.CountryOfBirthCode;
        document.IsProfessionalTutor = message.IsProfessionalTutor;
        document.IsSuperTutor = message.IsSuperTutor;
        document.IsAcceptingNewStudents = message.IsAcceptingNewStudents;
        document.HourlyRateMinorUnits = message.HourlyRateAmountMinorUnits;
        document.Currency = message.HourlyRateCurrency;
        document.PublishedAtUtc = message.PublishedAtUtc;
        document.IsPublished = true;

        await db.SaveChangesAsync(ct);
    }
}
