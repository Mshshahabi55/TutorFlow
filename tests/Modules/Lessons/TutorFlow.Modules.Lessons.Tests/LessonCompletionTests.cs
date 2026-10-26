using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Lessons.Domain;
using TutorFlow.Modules.Lessons.Persistence;
using CompleteDueLessonsFeature = TutorFlow.Modules.Lessons.Features.CompleteDueLessons.CompleteDueLessons;

namespace TutorFlow.Modules.Lessons.Tests;

// V1 completion rule (Product Owner approved, INTENTIONAL DEVIATION from Preply's Classroom-
// aware rule): EndUtc + 15 minutes, Classroom-independent. These tests exercise the real
// Testcontainers-backed LessonsDbContext (via the fixture) with a locally-controlled
// FakeTimeProvider, same precedent as BookingsApiFixture's DomainTests.cs — never a sleep, never
// a background timer, always a deterministic injected clock.
[Collection(LessonsHostCollection.Name)]
public class LessonCompletionTests(LessonsApiFixture fixture) : IClassFixture<LessonsApiFixture>
{
    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private async Task<(Guid LessonId, Guid StudentId)> SeedScheduledLessonAsync(DateTimeOffset endUtc)
    {
        var studentId = Guid.NewGuid();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        var lesson = Lesson.CreateScheduled(Guid.NewGuid(), studentId, Guid.NewGuid(), endUtc.AddMinutes(-25), endUtc, TimeProvider.System);
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (lesson.Id, studentId);
    }

    private async Task<LessonStatus> ReadStatusAsync(Guid lessonId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        return (await db.Lessons.AsNoTracking().SingleAsync(l => l.Id == lessonId, TestContext.Current.CancellationToken)).Status;
    }

    private async Task<int> RunSweepAsync(Guid studentId, DateTimeOffset asOf)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        var handler = new CompleteDueLessonsFeature.Handler(db, new FakeTimeProvider(asOf));
        return await handler.HandleAsync(studentId, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BeforeThreshold_RemainsScheduled()
    {
        var endUtc = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var (lessonId, studentId) = await SeedScheduledLessonAsync(endUtc);

        var affected = await RunSweepAsync(studentId, endUtc.AddMinutes(10));

        Assert.Equal(0, affected);
        Assert.Equal(LessonStatus.Scheduled, await ReadStatusAsync(lessonId));
    }

    [Fact]
    public async Task OneSecondBeforeThreshold_RemainsScheduled()
    {
        var endUtc = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var (lessonId, studentId) = await SeedScheduledLessonAsync(endUtc);

        var affected = await RunSweepAsync(studentId, endUtc.AddMinutes(15).AddSeconds(-1));

        Assert.Equal(0, affected);
        Assert.Equal(LessonStatus.Scheduled, await ReadStatusAsync(lessonId));
    }

    [Fact]
    public async Task ExactlyAtThreshold_BecomesCompleted()
    {
        var endUtc = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var (lessonId, studentId) = await SeedScheduledLessonAsync(endUtc);

        var affected = await RunSweepAsync(studentId, endUtc.AddMinutes(15));

        Assert.Equal(1, affected);
        Assert.Equal(LessonStatus.Completed, await ReadStatusAsync(lessonId));
    }

    [Fact]
    public async Task AfterThreshold_BecomesCompleted()
    {
        var endUtc = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var (lessonId, studentId) = await SeedScheduledLessonAsync(endUtc);

        var affected = await RunSweepAsync(studentId, endUtc.AddHours(2));

        Assert.Equal(1, affected);
        Assert.Equal(LessonStatus.Completed, await ReadStatusAsync(lessonId));
    }

    [Fact]
    public async Task HistoricalLesson_NeverPreviouslySwept_StillCompletesOnFirstRead()
    {
        // Requirement: works for historical lessons without a background job having run at the
        // exact threshold — this lesson "ended" long before any sweep ever ran against it.
        var endUtc = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var (lessonId, studentId) = await SeedScheduledLessonAsync(endUtc);

        var affected = await RunSweepAsync(studentId, new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(1, affected);
        Assert.Equal(LessonStatus.Completed, await ReadStatusAsync(lessonId));
    }

    [Fact]
    public async Task CancelledLesson_PastThreshold_NeverBecomesCompleted()
    {
        var endUtc = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var (lessonId, studentId) = await SeedScheduledLessonAsync(endUtc);

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
            // No CancelLesson feature exists yet (out of scope) — set up the Cancelled
            // precondition directly, the same conditional-UPDATE mechanism production code uses.
            await db.Lessons.Where(l => l.Id == lessonId)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, LessonStatus.Cancelled), TestContext.Current.CancellationToken);
        }

        var affected = await RunSweepAsync(studentId, endUtc.AddHours(2));

        Assert.Equal(0, affected);
        Assert.Equal(LessonStatus.Cancelled, await ReadStatusAsync(lessonId));
    }

    [Fact]
    public async Task RepeatedSweep_IsIdempotent_SecondCallAffectsNothing()
    {
        var endUtc = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var (lessonId, studentId) = await SeedScheduledLessonAsync(endUtc);
        var asOf = endUtc.AddHours(2);

        var first = await RunSweepAsync(studentId, asOf);
        var second = await RunSweepAsync(studentId, asOf);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(LessonStatus.Completed, await ReadStatusAsync(lessonId));
    }

    [Fact]
    public async Task ConcurrentSweeps_ForTheSameLesson_NeverDoubleTransition()
    {
        var endUtc = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var (lessonId, studentId) = await SeedScheduledLessonAsync(endUtc);
        var asOf = endUtc.AddHours(2);

        var results = await Task.WhenAll(
            RunSweepAsync(studentId, asOf),
            RunSweepAsync(studentId, asOf));

        // Exactly one of the two concurrent conditional UPDATEs can win the row (WHERE Status ==
        // Scheduled excludes it for whichever runs second) — the sum across both must be exactly
        // 1, never 2 (would mean the row was "completed twice") and never 0 (would mean neither
        // transitioned it).
        Assert.Equal(1, results[0] + results[1]);
        Assert.Equal(LessonStatus.Completed, await ReadStatusAsync(lessonId));
    }

    [Fact]
    public async Task FakeTimeProvider_ActuallyControlsTheThreshold_NotRealWallClock()
    {
        // A lesson ending far in the *future* relative to real wall-clock time, but the fake
        // clock is advanced past its threshold — if the real system clock were controlling this
        // instead of the injected TimeProvider, this lesson would incorrectly remain Scheduled.
        var endUtc = DateTimeOffset.UtcNow.AddYears(5);
        var (lessonId, studentId) = await SeedScheduledLessonAsync(endUtc);

        var affected = await RunSweepAsync(studentId, endUtc.AddHours(1));

        Assert.Equal(1, affected);
        Assert.Equal(LessonStatus.Completed, await ReadStatusAsync(lessonId));
    }

    [Fact]
    public async Task Sweep_OnlyAffectsTheRequestedStudentsOwnLessons()
    {
        var endUtc = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var (mineId, myStudentId) = await SeedScheduledLessonAsync(endUtc);
        var (otherId, _) = await SeedScheduledLessonAsync(endUtc);

        await RunSweepAsync(myStudentId, endUtc.AddHours(2));

        Assert.Equal(LessonStatus.Completed, await ReadStatusAsync(mineId));
        Assert.Equal(LessonStatus.Scheduled, await ReadStatusAsync(otherId));
    }
}
