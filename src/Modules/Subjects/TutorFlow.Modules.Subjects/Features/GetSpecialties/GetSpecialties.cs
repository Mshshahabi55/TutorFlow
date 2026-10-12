using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Subjects.Persistence;

namespace TutorFlow.Modules.Subjects.Features.GetSpecialties;

public static class GetSpecialties
{
    public sealed record Response(Guid Id, Guid SubjectId, string Code, string DisplayName);

    internal sealed class Handler(SubjectsDbContext db)
    {
        public async Task<IReadOnlyList<Response>> HandleAsync(Guid? subjectId, CancellationToken ct)
        {
            var query = db.Specialties.AsNoTracking().Where(s => s.IsActive);

            if (subjectId is { } id)
            {
                query = query.Where(s => s.SubjectId == id);
            }

            return await query
                .OrderBy(s => s.SortOrder)
                .Select(s => new Response(s.Id, s.SubjectId, s.Code, s.DisplayName))
                .ToListAsync(ct);
        }
    }
}
