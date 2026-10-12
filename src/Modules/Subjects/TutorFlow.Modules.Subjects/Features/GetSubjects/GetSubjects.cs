using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Subjects.Persistence;

namespace TutorFlow.Modules.Subjects.Features.GetSubjects;

public static class GetSubjects
{
    public sealed record Response(Guid Id, string Code, string DisplayName);

    internal sealed class Handler(SubjectsDbContext db)
    {
        public async Task<IReadOnlyList<Response>> HandleAsync(CancellationToken ct) =>
            await db.Subjects
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .Select(s => new Response(s.Id, s.Code, s.DisplayName))
                .ToListAsync(ct);
    }
}
