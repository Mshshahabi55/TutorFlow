using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Identity.Repositories;

// Real persistence (docs/adr/ADR-013-persistence-technology.md). Never
// validates invariants or business rules — those belong to the Domain
// aggregate alone.
internal sealed class TutorRepository : ITutorRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public TutorRepository(TutorFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Tracked (not AsNoTracking): every caller of GetByIdAsync/GetByEmailAsync
    // that matters for correctness is a command handler that mutates the
    // returned Tutor and passes it to IUnitOfWork.SaveChangesAsync
    // (Approve/Suspend/SetHourlyRate/SetSubject/SetLanguage/SetLocation/
    // SetOfferedDurations/Login/AdminResetPassword) — EfUnitOfWork never
    // re-attaches aggregates itself (docs/phases/PHASE-01B-REPORT.md Section
    // 3/6), so an untracked read here meant the mutation was silently never
    // persisted. GetTutorByIdQueryHandler's read-only usage of GetByIdAsync
    // pays a small, acceptable tracking overhead as a side effect of sharing
    // this method with the command path, rather than this repository
    // maintaining two near-duplicate lookups for the same entity.
    public Task<Tutor?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
        _dbContext.Tutors.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Tutor?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default) =>
        _dbContext.Tutors.FirstOrDefaultAsync(t => t.Email == email, cancellationToken);

    public async Task<IReadOnlyCollection<Tutor>> GetDiscoverableAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Tutors
            .AsNoTracking()
            .Where(t => t.IsApproved && !t.IsSuspended)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyCollection<Tutor> Items, int TotalCount)> GetPendingAsync(
        PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tutors.AsNoTracking().Where(t => !t.IsApproved);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageRequest.Page - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyCollection<Tutor>> SearchDiscoverableAsync(
        string? subject, string? language, string? location, CancellationToken cancellationToken = default)
    {
        var discoverable = await _dbContext.Tutors
            .AsNoTracking()
            .Where(t => t.IsApproved && !t.IsSuspended)
            .ToListAsync(cancellationToken);

        return discoverable.Where(t =>
            (subject is null || (t.Subject is not null && string.Equals(t.Subject.Value, subject, StringComparison.OrdinalIgnoreCase))) &&
            (language is null || (t.Language is not null && string.Equals(t.Language.Value, language, StringComparison.OrdinalIgnoreCase))) &&
            (location is null || (t.Location is not null && string.Equals(t.Location.Value, location, StringComparison.OrdinalIgnoreCase))))
            .ToList();
    }

    public Task AddAsync(Tutor tutor, CancellationToken cancellationToken = default)
    {
        _dbContext.Tutors.Add(tutor);
        return Task.CompletedTask;
    }
}
