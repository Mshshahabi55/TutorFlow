using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Modules.Lessons.Domain;
using TutorFlow.Modules.Lessons.Persistence;
using TutorFlow.Shared;
using CreateLessonFromBookingFeature = TutorFlow.Modules.Lessons.Features.CreateLessonFromBooking.CreateLessonFromBooking;

namespace TutorFlow.Modules.Lessons.Tests;

public class LessonsPipelineTests(LessonsApiFixture fixture) : IClassFixture<LessonsApiFixture>
{
    // PostgreSQL's "timestamp with time zone" is microsecond-precision; .NET's DateTimeOffset
    // is tick-precision (100ns) — DateTimeOffset.UtcNow's sub-microsecond ticks would otherwise
    // make an exact round-trip equality assertion flaky depending on wall-clock timing.
    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value) =>
        value.AddTicks(-(value.Ticks % 10));

    private async Task<Guid> SeedBookingAsync(Guid studentUserId, Guid tutorUserId, DateTimeOffset? startUtc = null)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var booking = Booking.CreateHeld(
            studentUserId, tutorUserId, 25, startUtc ?? DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddMinutes(10), new TutorFlow.Modules.Bookings.Domain.Money(2500, "EUR"),
            TimeProvider.System, isTrial: true).Value;
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.Id;
    }

    [Fact]
    public async Task HandleAsync_WithValidBookingData_CreatesExactlyOneScheduledLesson()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var bookingId = await SeedBookingAsync(studentId, tutorId);
        var startUtc = TruncateToMicroseconds(DateTimeOffset.UtcNow.AddDays(2));
        var endUtc = startUtc.AddMinutes(25);

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateLessonFromBookingFeature.Handler>();

        var outcome = await handler.HandleAsync(bookingId, studentId, tutorId, startUtc, endUtc, TestContext.Current.CancellationToken);

        Assert.Equal(CreateLessonFromBookingFeature.Outcome.Created, outcome);

        var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        var lesson = await db.Lessons.AsNoTracking().SingleAsync(l => l.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.Equal(studentId, lesson.StudentUserId);
        Assert.Equal(tutorId, lesson.TutorUserId);
        Assert.Equal(startUtc, lesson.StartUtc);
        Assert.Equal(endUtc, lesson.EndUtc);
        Assert.Equal(LessonStatus.Scheduled, lesson.Status);
    }

    [Fact]
    public async Task HandleAsync_CalledTwiceForTheSameBooking_IsIdempotent_NeverCreatesASecondLesson()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var bookingId = await SeedBookingAsync(studentId, tutorId);
        var startUtc = DateTimeOffset.UtcNow.AddDays(2);
        var endUtc = startUtc.AddMinutes(25);

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateLessonFromBookingFeature.Handler>();

        var first = await handler.HandleAsync(bookingId, studentId, tutorId, startUtc, endUtc, TestContext.Current.CancellationToken);
        var second = await handler.HandleAsync(bookingId, studentId, tutorId, startUtc, endUtc, TestContext.Current.CancellationToken);

        Assert.Equal(CreateLessonFromBookingFeature.Outcome.Created, first);
        Assert.Equal(CreateLessonFromBookingFeature.Outcome.AlreadyExists, second);

        var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        Assert.Equal(1, await db.Lessons.CountAsync(l => l.BookingId == bookingId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_ConcurrentDuplicateDelivery_OnlyOneLessonIsEverCreated()
    {
        // Simulates Wolverine's at-least-once redelivery arriving as two genuinely concurrent
        // calls — the DB's unique index on BookingId is the real guard, not the pre-check
        // (same shape as Payments' CreateIntent_ConcurrentRequestsForTheSameBooking test).
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var bookingId = await SeedBookingAsync(studentId, tutorId);
        var startUtc = DateTimeOffset.UtcNow.AddDays(2);
        var endUtc = startUtc.AddMinutes(25);

        using var scopeA = fixture.Services.CreateScope();
        using var scopeB = fixture.Services.CreateScope();
        var handlerA = scopeA.ServiceProvider.GetRequiredService<CreateLessonFromBookingFeature.Handler>();
        var handlerB = scopeB.ServiceProvider.GetRequiredService<CreateLessonFromBookingFeature.Handler>();

        var outcomes = await Task.WhenAll(
            handlerA.HandleAsync(bookingId, studentId, tutorId, startUtc, endUtc, TestContext.Current.CancellationToken),
            handlerB.HandleAsync(bookingId, studentId, tutorId, startUtc, endUtc, TestContext.Current.CancellationToken));

        Assert.Contains(CreateLessonFromBookingFeature.Outcome.Created, outcomes);

        using var finalScope = fixture.Services.CreateScope();
        var db = finalScope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        Assert.Equal(1, await db.Lessons.CountAsync(l => l.BookingId == bookingId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_ForTwoDifferentBookings_CreatesTwoIndependentLessons()
    {
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var bookingOneId = await SeedBookingAsync(studentId, tutorId);
        var bookingTwoId = await SeedBookingAsync(studentId, tutorId, DateTimeOffset.UtcNow.AddDays(5));

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateLessonFromBookingFeature.Handler>();

        await handler.HandleAsync(bookingOneId, studentId, tutorId, DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(2).AddMinutes(25), TestContext.Current.CancellationToken);
        await handler.HandleAsync(bookingTwoId, studentId, tutorId, DateTimeOffset.UtcNow.AddDays(5), DateTimeOffset.UtcNow.AddDays(5).AddMinutes(25), TestContext.Current.CancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();
        Assert.Equal(1, await db.Lessons.CountAsync(l => l.BookingId == bookingOneId, TestContext.Current.CancellationToken));
        Assert.Equal(1, await db.Lessons.CountAsync(l => l.BookingId == bookingTwoId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RawConflictingInsert_BypassingApplicationCode_IsRejectedByDatabaseItself()
    {
        // Proves the unique constraint exists and works independent of application code —
        // same rigor as Bookings' own exclusion-constraint DB-level test.
        var bookingId = Guid.NewGuid();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LessonsDbContext>();

        db.Lessons.Add(Lesson.CreateScheduled(bookingId, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddMinutes(25), TimeProvider.System));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.ChangeTracker.Clear();
        db.Lessons.Add(Lesson.CreateScheduled(bookingId, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(2).AddMinutes(25), TimeProvider.System));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BookingConfirmedConsumer_IsDiscoveredAndExecutable_ThroughTheRealWolverinePipeline()
    {
        // Composition-root DI resolution sanity test (BOOKINGS_SPEC.md §9 precedent) — publishes
        // a real BookingConfirmed via the real IMessageBus and confirms Wolverine actually
        // discovered and dispatched to BookingConfirmedConsumer, not merely that the consumer
        // class compiles.
        var studentId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var bookingId = await SeedBookingAsync(studentId, tutorId);
        var startUtc = DateTimeOffset.UtcNow.AddDays(2);
        var endUtc = startUtc.AddMinutes(25);

        using (var scope = fixture.Services.CreateScope())
        {
            // Wolverine's generated handler-construction code is internal to its own dispatch
            // pipeline, not a standalone DI-resolvable service — BookingConfirmedConsumer itself
            // is never meant to be fetched via GetService. The real proof that it was discovered
            // and wired is the downstream Lesson row the poll below asserts on: if Wolverine's
            // registration-order regressed, that row would simply never appear.
            var bus = scope.ServiceProvider.GetRequiredService<Wolverine.IMessageBus>();
            await bus.PublishAsync(new BookingConfirmed(bookingId, studentId, tutorId, startUtc, endUtc, 25, null, DateTimeOffset.UtcNow));
        }

        var deadline = DateTime.UtcNow.AddSeconds(20);
        Lesson? lesson = null;
        while (DateTime.UtcNow < deadline && lesson is null)
        {
            using var pollScope = fixture.Services.CreateScope();
            var db = pollScope.ServiceProvider.GetRequiredService<LessonsDbContext>();
            lesson = await db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.BookingId == bookingId, TestContext.Current.CancellationToken);
            if (lesson is null)
            {
                await Task.Delay(100, TestContext.Current.CancellationToken);
            }
        }

        Assert.NotNull(lesson);
    }
}
