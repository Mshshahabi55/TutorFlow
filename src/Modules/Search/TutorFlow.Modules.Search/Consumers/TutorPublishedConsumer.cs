using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Search.Domain;
using TutorFlow.Modules.Search.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Search.Consumers;

/// <summary>
/// Wolverine discovers this by convention (a public <c>HandleAsync</c> instance method) — no
/// interface or registration needed beyond <c>opts.Discovery.IncludeAssembly</c> in
/// <c>TutorFlow.Host/Program.cs</c>. Constructor-injected <c>SearchDbContext</c>, matching every
/// other Handler class in this codebase. <c>SearchDbContext</c> is explicitly allow-listed for
/// service location in <c>Program.cs</c> (<c>AlwaysUseServiceLocationFor</c>) — <c>AddDbContext</c>
/// registers <c>DbContextOptions&lt;T&gt;</c> via an opaque lambda factory that Wolverine 6's
/// codegen otherwise refuses to resolve (<c>InvalidServiceLocationException</c> at startup).
/// Upserts the one row <c>TutorPublished</c> describes; never touches any other tutor's document.
/// </summary>
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
