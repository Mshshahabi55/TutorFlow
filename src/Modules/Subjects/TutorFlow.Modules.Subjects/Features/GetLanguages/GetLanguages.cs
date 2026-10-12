using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Subjects.Persistence;

namespace TutorFlow.Modules.Subjects.Features.GetLanguages;

public static class GetLanguages
{
    public sealed record Response(string Code, string DisplayName);

    internal sealed class Handler(SubjectsDbContext db)
    {
        public async Task<IReadOnlyList<Response>> HandleAsync(CancellationToken ct) =>
            await db.Languages
                .AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.SortOrder)
                .Select(l => new Response(l.Code, l.DisplayName))
                .ToListAsync(ct);
    }
}
