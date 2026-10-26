using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Search.Consumers;
using TutorFlow.Modules.Search.Domain;
using TutorFlow.Modules.Search.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Search.Tests;

/// <summary>
/// Regression coverage for the ordering guard in <see cref="TutorAvailabilitySignalChangedConsumer"/>:
/// Wolverine's local in-process transport does not guarantee in-order delivery of two
/// <see cref="TutorAvailabilitySignalChanged"/> events for the same tutor, and a plain
/// unconditional overwrite previously let an older event processed after a newer one revert
/// <see cref="TutorSearchDocument.HasUpcomingAvailability"/> to a stale value (observed as an
/// intermittent failure in <c>TutorPublishedPipelineTests.UpdatingWeeklyRules_...</c>).
/// </summary>
[Collection(SearchHostCollection.Name)]
public sealed class TutorAvailabilitySignalChangedConsumerTests(SearchApiFixture fixture) : IClassFixture<SearchApiFixture>
{
    private static readonly DateTimeOffset T1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = T1.AddSeconds(5);

    private async Task<Guid> SeedDocumentAsync(bool hasUpcomingAvailability = false, DateTimeOffset? signalOccurredAtUtc = null)
    {
        var tutorUserId = Guid.NewGuid();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        db.TutorSearchDocuments.Add(new TutorSearchDocument
        {
            TutorUserId = tutorUserId,
            DisplayName = "Signal Ordering Test Tutor",
            HasUpcomingAvailability = hasUpcomingAvailability,
            AvailabilitySignalOccurredAtUtc = signalOccurredAtUtc,
            IsPublished = true,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return tutorUserId;
    }

    private async Task HandleAsync(Guid tutorUserId, bool hasUpcomingAvailability, DateTimeOffset occurredAtUtc)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        var consumer = new TutorAvailabilitySignalChangedConsumer(db);
        await consumer.HandleAsync(
            new TutorAvailabilitySignalChanged(tutorUserId, hasUpcomingAvailability, occurredAtUtc),
            TestContext.Current.CancellationToken);
    }

    private async Task<TutorSearchDocument> ReadDocumentAsync(Guid tutorUserId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        return await db.TutorSearchDocuments.AsNoTracking().SingleAsync(
            d => d.TutorUserId == tutorUserId, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FirstSignal_WithNoPriorTimestamp_IsApplied()
    {
        var tutorUserId = await SeedDocumentAsync(hasUpcomingAvailability: false, signalOccurredAtUtc: null);

        await HandleAsync(tutorUserId, hasUpcomingAvailability: true, occurredAtUtc: T1);

        var document = await ReadDocumentAsync(tutorUserId);
        Assert.True(document.HasUpcomingAvailability);
        Assert.Equal(T1, document.AvailabilitySignalOccurredAtUtc);
    }

    [Fact]
    public async Task NewerSignal_ReplacesAnAlreadyAcceptedOlderSignal()
    {
        var tutorUserId = await SeedDocumentAsync(hasUpcomingAvailability: false, signalOccurredAtUtc: T1);

        await HandleAsync(tutorUserId, hasUpcomingAvailability: true, occurredAtUtc: T2);

        var document = await ReadDocumentAsync(tutorUserId);
        Assert.True(document.HasUpcomingAvailability);
        Assert.Equal(T2, document.AvailabilitySignalOccurredAtUtc);
    }

    [Fact]
    public async Task OlderSignal_ArrivingAfterANewerAcceptedSignal_CannotOverwriteIt()
    {
        // Accept the newer signal first, exactly as can happen when Wolverine delivers the two
        // events out of publish-order.
        var tutorUserId = await SeedDocumentAsync(hasUpcomingAvailability: false, signalOccurredAtUtc: null);
        await HandleAsync(tutorUserId, hasUpcomingAvailability: true, occurredAtUtc: T2);

        await HandleAsync(tutorUserId, hasUpcomingAvailability: false, occurredAtUtc: T1);

        // Both the value AND the stored timestamp remain exactly as the newer signal left them —
        // the stale event must not partially apply either field.
        var document = await ReadDocumentAsync(tutorUserId);
        Assert.True(document.HasUpcomingAvailability);
        Assert.Equal(T2, document.AvailabilitySignalOccurredAtUtc);
    }

    [Fact]
    public async Task EqualTimestamp_RedeliveryOfTheIdenticalEvent_IsIdempotent()
    {
        var tutorUserId = await SeedDocumentAsync(hasUpcomingAvailability: false, signalOccurredAtUtc: null);

        await HandleAsync(tutorUserId, hasUpcomingAvailability: true, occurredAtUtc: T1);
        await HandleAsync(tutorUserId, hasUpcomingAvailability: true, occurredAtUtc: T1);

        var document = await ReadDocumentAsync(tutorUserId);
        Assert.True(document.HasUpcomingAvailability);
        Assert.Equal(T1, document.AvailabilitySignalOccurredAtUtc);
    }

    [Fact]
    public async Task EqualTimestamp_WithADifferentValue_IsAcceptedPerTheChosenLastWriteAtSameInstantPolicy()
    {
        // Two distinct events sharing the exact same OccurredAtUtc can't be ordered by timestamp
        // at all; the chosen, documented policy is "same-or-newer is accepted" (the guard uses
        // <=), so the later-processed one wins rather than silently being dropped.
        var tutorUserId = await SeedDocumentAsync(hasUpcomingAvailability: false, signalOccurredAtUtc: T1);

        await HandleAsync(tutorUserId, hasUpcomingAvailability: true, occurredAtUtc: T1);

        var document = await ReadDocumentAsync(tutorUserId);
        Assert.True(document.HasUpcomingAvailability);
        Assert.Equal(T1, document.AvailabilitySignalOccurredAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenNoDocumentExistsYet_IsANoOp()
    {
        // Preserves the pre-existing documented contract: TutorPublished sets the real baseline;
        // this consumer never creates a document on its own.
        var tutorUserId = Guid.NewGuid();

        await HandleAsync(tutorUserId, hasUpcomingAvailability: true, occurredAtUtc: T1);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        var exists = await db.TutorSearchDocuments.AnyAsync(
            d => d.TutorUserId == tutorUserId, TestContext.Current.CancellationToken);
        Assert.False(exists);
    }

    [Fact]
    public async Task ConcurrentDelivery_OfAnOlderAndANewerSignal_TheNewerAlwaysWinsRegardlessOfArrivalOrder()
    {
        // Real concurrent execution (not sequential reordering) against the same row, from two
        // independent scopes/DbContexts/connections — exercises Postgres' row-level locking and
        // predicate re-evaluation under the WHERE guard, not just the application-level logic.
        // Repeated to raise confidence this holds regardless of which task's statement the
        // database happens to execute first.
        for (var iteration = 0; iteration < 10; iteration++)
        {
            var tutorUserId = await SeedDocumentAsync(hasUpcomingAvailability: false, signalOccurredAtUtc: null);

            var older = HandleAsync(tutorUserId, hasUpcomingAvailability: false, occurredAtUtc: T1);
            var newer = HandleAsync(tutorUserId, hasUpcomingAvailability: true, occurredAtUtc: T2);
            await Task.WhenAll(older, newer);

            var document = await ReadDocumentAsync(tutorUserId);
            Assert.True(document.HasUpcomingAvailability);
            Assert.Equal(T2, document.AvailabilitySignalOccurredAtUtc);
        }
    }
}
