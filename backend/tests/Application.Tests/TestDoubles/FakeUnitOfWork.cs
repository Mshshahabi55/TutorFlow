using TutorFlow.Application.Common;
using TutorFlow.Domain.Common;

namespace TutorFlow.Application.Tests.TestDoubles;

// Minimal hand-rolled test double — no mocking framework. Tracks how many
// times SaveChangesAsync was called so tests can verify handlers actually
// commit their work. Mirrors EfUnitOfWork's event-clearing behavior so
// tests observe the same lifecycle guarantee as production.
//
// WHAT THIS PROVES, AND WHAT IT DOES NOT (docs/phases/PHASE-01B-REPORT.md,
// PHASE-01C-REPORT.md Section 6): this double proves handler ORCHESTRATION
// only — that a handler validated its input, checked authorization,
// mutated the aggregate it was supposed to, and called SaveChangesAsync
// the expected number of times. It does NOT and CANNOT prove persistence.
// It has no concept of EF Core change tracking, so it cannot distinguish
// "this mutation will actually reach the database" from "this mutation
// exists only in a local variable and is about to vanish" — which is
// exactly the difference that mattered when TutorRepository.GetByIdAsync/
// GetByEmailAsync were AsNoTracking(): every Application.Tests test for
// Approve/Suspend/SetHourlyRate/SetSubject/SetLanguage/SetLocation/
// SetOfferedDurations/Login/AdminResetPassword passed the whole time,
// because this double only checks that SaveChangesAsync was called, never
// whether a real EfUnitOfWork given the same aggregate would have produced
// an UPDATE statement or a silent no-op.
//
// Any claim that a write persists must be proven in Web.Tests, by
// re-reading the entity from a fresh DbContext scope
// (backend/tests/Web.Tests/PersistenceIntegrityTests.cs is the reference
// shape) — never by asserting SaveChangesCallCount here, and never by
// asserting an in-memory aggregate's own property changed, since that is
// true whether or not the mutation was ever going to be saved.
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(IEnumerable<IAggregateRoot> touchedAggregates, CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;

        foreach (var aggregate in touchedAggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return Task.FromResult(0);
    }
}
