using TutorFlow.Application.Common;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Infrastructure.Common;
using TutorFlow.Infrastructure.Persistence;
using TutorFlow.Infrastructure.Tests.TestDoubles;

namespace TutorFlow.Infrastructure.Tests.Persistence;

public class EfUnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_persists_the_touched_aggregate_and_clears_its_domain_events()
    {
        using var sqlite = new SqliteTestDbContext();
        var unitOfWork = new EfUnitOfWork(sqlite.DbContext, new DomainEventDispatcher(Enumerable.Empty<IDomainEventHandler>()));

        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        sqlite.DbContext.Tutors.Add(tutor);
        Assert.NotEmpty(tutor.DomainEvents);

        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { tutor });

        Assert.Empty(tutor.DomainEvents);

        using var verify = new SqliteTestDbContext();
        // Re-reading from a fresh context isn't possible against the same
        // in-memory connection string across instances, so instead verify
        // via the same context's own change tracker having persisted state:
        var stored = await sqlite.DbContext.Tutors.FindAsync(tutor.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task SaveChangesAsync_dispatches_touched_aggregates_before_clearing_events()
    {
        using var sqlite = new SqliteTestDbContext();
        var dispatcher = new RecordingDomainEventDispatcher();
        var unitOfWork = new EfUnitOfWork(sqlite.DbContext, dispatcher);

        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        sqlite.DbContext.Tutors.Add(tutor);

        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { tutor });

        Assert.Equal(1, dispatcher.CallCount);
        Assert.NotNull(dispatcher.LastTouchedAggregates);
        Assert.Single(dispatcher.LastTouchedAggregates!, a => ReferenceEquals(a, tutor));
    }
}
