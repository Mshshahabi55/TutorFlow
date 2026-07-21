using TutorFlow.Application.Audit.DTOs;
using TutorFlow.Application.Audit.Handlers;
using TutorFlow.Application.Audit.Queries;
using TutorFlow.Application.Tests.TestDoubles;

namespace TutorFlow.Application.Tests.Audit;

public class GetAuditEntriesQueryHandlerTests
{
    private static AuditEntryDto MakeEntry(Guid subjectId, string action, DateTime occurredOnUtc) =>
        new(Guid.NewGuid(), occurredOnUtc, action, subjectId, "actor-1", "Tutor");

    [Fact]
    public async Task Handle_returns_every_entry_when_no_subject_filter_is_given()
    {
        var repository = new InMemoryAuditEntryRepository();
        repository.Add(MakeEntry(Guid.NewGuid(), "SessionBooked", DateTime.UtcNow));
        repository.Add(MakeEntry(Guid.NewGuid(), "TutorApproved", DateTime.UtcNow));
        var handler = new GetAuditEntriesQueryHandler(repository);

        var result = await handler.Handle(new GetAuditEntriesQuery(null, 1, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_filters_by_subject_id_when_provided()
    {
        var repository = new InMemoryAuditEntryRepository();
        var subjectId = Guid.NewGuid();
        repository.Add(MakeEntry(subjectId, "SessionBooked", DateTime.UtcNow));
        repository.Add(MakeEntry(Guid.NewGuid(), "TutorApproved", DateTime.UtcNow));
        var handler = new GetAuditEntriesQueryHandler(repository);

        var result = await handler.Handle(new GetAuditEntriesQuery(subjectId, 1, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(subjectId, result.Value.Items.Single().SubjectId);
    }

    [Fact]
    public async Task Handle_paginates_the_result()
    {
        var repository = new InMemoryAuditEntryRepository();
        for (var i = 0; i < 3; i++)
        {
            repository.Add(MakeEntry(Guid.NewGuid(), "SessionBooked", DateTime.UtcNow.AddMinutes(-i)));
        }
        var handler = new GetAuditEntriesQueryHandler(repository);

        var result = await handler.Handle(new GetAuditEntriesQuery(null, 1, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task Handle_returns_failure_for_empty_subject_id()
    {
        var repository = new InMemoryAuditEntryRepository();
        var handler = new GetAuditEntriesQueryHandler(repository);

        var result = await handler.Handle(new GetAuditEntriesQuery(Guid.Empty, 1, 20));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_returns_failure_for_page_less_than_one()
    {
        var repository = new InMemoryAuditEntryRepository();
        var handler = new GetAuditEntriesQueryHandler(repository);

        var result = await handler.Handle(new GetAuditEntriesQuery(null, 0, 20));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_returns_failure_for_page_size_out_of_range()
    {
        var repository = new InMemoryAuditEntryRepository();
        var handler = new GetAuditEntriesQueryHandler(repository);

        var result = await handler.Handle(new GetAuditEntriesQuery(null, 1, 1000));

        Assert.True(result.IsFailure);
    }
}
