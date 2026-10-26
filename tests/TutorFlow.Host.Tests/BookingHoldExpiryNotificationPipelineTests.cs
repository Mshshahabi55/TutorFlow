using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Infrastructure;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Notifications.Domain;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Modules.Notifications.Persistence;
using TutorFlow.Modules.Tutors.Persistence;
using BookingMoney = TutorFlow.Modules.Bookings.Domain.Money;
using TutorMoney = TutorFlow.Modules.Tutors.Domain.Money;
using TutorApplication = TutorFlow.Modules.Tutors.Domain.TutorApplication;
using DraftUpdate = TutorFlow.Modules.Tutors.Domain.DraftUpdate;
using TutorSpokenLanguage = TutorFlow.Modules.Tutors.Domain.TutorSpokenLanguage;
using LanguageProficiency = TutorFlow.Modules.Tutors.Domain.LanguageProficiency;
using TutorFlow.Shared;
using Wolverine;
using Wolverine.Tracking;

namespace TutorFlow.Host.Tests;

/// <summary>
/// ADR-0029: the full real pipeline — Bookings' sweep publishes <c>BookingHoldExpired</c> via
/// Wolverine, Notifications' real consumer receives it and attempts a real send — proven
/// end-to-end, no mocked cross-module boundary.
/// </summary>
[Collection(HostCollection.Name)]
public sealed class BookingHoldExpiryNotificationPipelineTests(BookingHoldExpiryNotificationPipelineFixture fixture)
    : IClassFixture<BookingHoldExpiryNotificationPipelineFixture>
{
    [Fact]
    public async Task ExpiringAHeldBooking_ViaTheRealSweep_ResultsInARealEmailSendAttempt_ViaWolverine()
    {
        var clock = TimeProvider.System;
        Guid bookingId;
        Guid studentUserId;
        const string studentEmail = "pipeline-student@example.com";
        const string tutorDisplayName = "Pipeline Tutor";

        using (var scope = fixture.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var student = new ApplicationUser { Id = Guid.NewGuid(), UserName = studentEmail, Email = studentEmail, CreatedAtUtc = clock.GetUtcNow() };
            var createResult = await userManager.CreateAsync(student, "Password123!");
            Assert.True(createResult.Succeeded);
            studentUserId = student.Id;

            var tutorsDb = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var application = TutorApplication.CreateDraft(Guid.NewGuid(), clock);
            application.ApplyDraftUpdate(
                new DraftUpdate(
                    DisplayName: tutorDisplayName,
                    Headline: "Patient tutor",
                    ProfilePhotoReference: "https://example.com/photo.jpg",
                    IntroVideoReference: "https://example.com/intro.mp4",
                    SubjectIds: [Guid.NewGuid()],
                    SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                    HourlyRate: new TutorMoney(2500, "EUR")),
                clock);
            tutorsDb.TutorApplications.Add(application);
            application.Submit(clock);
            await tutorsDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            var tutorUserId = application.UserId;

            var bookingsDb = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var bookingResult = Booking.CreateHeld(
                studentUserId, tutorUserId, 25, clock.GetUtcNow().AddDays(1), clock.GetUtcNow().AddMinutes(-5),
                new BookingMoney(2500, "EUR"), clock, isTrial: true);
            Assert.True(bookingResult.IsSuccess);
            bookingsDb.Bookings.Add(bookingResult.Value);
            await bookingsDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            bookingId = bookingResult.Value.Id;
        }

        ITrackedSession trackedSession;
        using (var scope = fixture.Services.CreateScope())
        {
            var sweepService = scope.ServiceProvider.GetRequiredService<BookingHoldExpirySweepService>();

            // Wolverine's local in-process transport delivers asynchronously — PublishAsync only
            // guarantees the message was routed, not that the consumer has run (directly measured
            // across several batches: the real dispatch-to-execution gap can be several seconds on
            // the first message of a given type in a fresh host, racing a fixed-duration DB poll).
            // TrackActivity/ExecuteAndWaitAsync waits deterministically for the entire message
            // cascade to reach a terminal state (success, failure, or timeout) instead.
            trackedSession = await fixture.Services.TrackActivity(TimeSpan.FromSeconds(30))
                .ExecuteAndWaitAsync(_ => sweepService.SweepOnceAsync(TestContext.Current.CancellationToken));
        }

        // Wolverine's own default assertion (ExecuteAndWaitAsync throws on a detected timeout or
        // exception unless explicitly opted out) already guards against a failed/stranded
        // dispatch; this positively confirms the specific expected message reached the
        // successful-completion record, not merely that nothing else went wrong.
        var succeededEnvelope = trackedSession.MessageSucceeded.SingleEnvelope<BookingHoldExpired>();
        Assert.NotNull(succeededEnvelope);

        using (var scope = fixture.Services.CreateScope())
        {
            var bookingsDb = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var status = await bookingsDb.Bookings.AsNoTracking().Where(b => b.Id == bookingId).Select(b => b.Status).SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(BookingStatus.Expired, status);
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstOrDefaultAsync(
                e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
            Assert.NotNull(claim);
            Assert.True(claim.WasSent);
        }

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        var sent = Assert.Single(sender.SentMessages, m => m.ToEmail == studentEmail);
        Assert.Contains(tutorDisplayName, sent.Subject);
    }

    /// <summary>
    /// A44 #103 architectural verification: <c>BookingHoldExpirySweepService</c>'s dispatch phase
    /// has no claim/lock of its own on the "not yet dispatched" read — only the claim phase's
    /// conditional UPDATE and the intent table's unique-on-BookingId index are guarded, and
    /// neither applies to dispatch at all. Genuinely concurrent <c>Task.WhenAll</c> calls to
    /// <c>SweepOnceAsync</c> turned out to be a timing-dependent, flaky way to prove this (the two
    /// calls do not reliably interleave before either writes <c>DispatchedAtUtc</c> — observed
    /// passing with 2 sends in isolation and with only 1 send under full-suite load). This test
    /// instead reproduces the exact query-then-publish-then-mark sequence
    /// <c>DispatchPendingIntentsAsync</c> runs, across two independent scopes with the ordering
    /// explicitly controlled (both reads happen before either write) — a deterministic
    /// reproduction of the real race's necessary condition, not a re-implementation of unrelated
    /// logic: the method under test has no claim/lock to bypass, so two readers both seeing one
    /// pending row before either marks it is the entire vulnerability, and is exactly what is
    /// constructed here. The real consumer
    /// (<c>SendAbandonedCheckoutRecoveryEmail.Handler</c>) is then proven genuinely idempotent
    /// against the resulting duplicate delivery — both sends succeed, neither errors, and exactly
    /// one <c>SentAbandonedCheckoutEmail</c> claim row survives.
    /// </summary>
    [Fact]
    public async Task TwoReadersObservingTheSamePendingIntentBeforeEitherDispatches_BothPublish_ButTheRealConsumerStaysIdempotent()
    {
        var clock = TimeProvider.System;
        Guid bookingId;
        Guid studentUserId, tutorUserId;
        DateTimeOffset startUtc, endUtc;
        int durationMinutes;
        bool isTrial;

        using (var scope = fixture.Services.CreateScope())
        {
            var bookingsDb = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var now = clock.GetUtcNow();
            var bookingResult = Booking.CreateHeld(
                Guid.NewGuid(), Guid.NewGuid(), 25, now.AddDays(1), now.AddMinutes(-5),
                new BookingMoney(2500, "EUR"), clock, isTrial: true);
            Assert.True(bookingResult.IsSuccess);
            var booking = bookingResult.Value;
            bookingsDb.Bookings.Add(booking);
            await bookingsDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            bookingId = booking.Id;
            studentUserId = booking.StudentUserId;
            tutorUserId = booking.TutorUserId;
            startUtc = booking.StartUtc;
            endUtc = booking.EndUtc;
            durationMinutes = booking.DurationMinutes;
            isTrial = booking.IsTrial;

            // Simulates a crash between a prior process's claim (status change + intent row,
            // already committed) and its own dispatch — the intent is left durably "owed."
            await bookingsDb.Bookings.Where(b => b.Id == bookingId).ExecuteUpdateAsync(
                s => s.SetProperty(b => b.Status, BookingStatus.Expired).SetProperty(b => b.HoldExpiresAtUtc, (DateTimeOffset?)null),
                TestContext.Current.CancellationToken);
            bookingsDb.BookingHoldExpiryIntents.Add(BookingHoldExpiryIntent.Create(
                bookingId, studentUserId, tutorUserId, startUtc, endUtc, durationMinutes, isTrial, now));
            await bookingsDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Two independent scopes/DbContext instances, standing in for two concurrent dispatchers.
        // Both reads happen here, before either writer below runs — deterministically
        // constructing the exact precondition a real race would need, instead of hoping
        // Task.WhenAll schedules them that way.
        using var scopeA = fixture.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var pendingA = await dbA.BookingHoldExpiryIntents
            .Where(i => i.DispatchedAtUtc == null && i.BookingId == bookingId)
            .ToListAsync(TestContext.Current.CancellationToken);
        var intentA = Assert.Single(pendingA);

        using var scopeB = fixture.Services.CreateScope();
        var dbB = scopeB.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var pendingB = await dbB.BookingHoldExpiryIntents
            .Where(i => i.DispatchedAtUtc == null && i.BookingId == bookingId)
            .ToListAsync(TestContext.Current.CancellationToken);
        var intentB = Assert.Single(pendingB);

        Func<Wolverine.IMessageContext, Task> raceBothDispatchers = async _ =>
        {
            // Replicates DispatchPendingIntentsAsync's own per-intent body exactly (publish,
            // then mark, then save) — first for reader A's copy, then reader B's, proving that
            // B's independently-fetched "pending" view is stale but still acted on, because
            // nothing re-checks it before publishing.
            var busA = scopeA.ServiceProvider.GetRequiredService<IMessageBus>();
            await busA.PublishAsync(new BookingHoldExpired(
                intentA.BookingId, intentA.StudentUserId, intentA.TutorUserId,
                intentA.StartUtc, intentA.EndUtc, intentA.DurationMinutes, intentA.IsTrial, intentA.ExpiredAtUtc));
            intentA.MarkDispatched(clock.GetUtcNow());
            await dbA.SaveChangesAsync(TestContext.Current.CancellationToken);

            var busB = scopeB.ServiceProvider.GetRequiredService<IMessageBus>();
            await busB.PublishAsync(new BookingHoldExpired(
                intentB.BookingId, intentB.StudentUserId, intentB.TutorUserId,
                intentB.StartUtc, intentB.EndUtc, intentB.DurationMinutes, intentB.IsTrial, intentB.ExpiredAtUtc));
            intentB.MarkDispatched(clock.GetUtcNow());
            await dbB.SaveChangesAsync(TestContext.Current.CancellationToken);
        };
        ITrackedSession trackedSession = await fixture.Services.TrackActivity(TimeSpan.FromSeconds(30))
            .ExecuteAndWaitAsync(raceBothDispatchers);

        // The decisive, non-inferred result: the real message bus sent the event twice for this
        // one intent, because nothing stopped reader B from acting on its stale "still pending"
        // view.
        Assert.Equal(2, trackedSession.Sent.MessagesOf<BookingHoldExpired>().Count(e => e.BookingId == bookingId));

        // Both deliveries reached the real consumer and neither was moved to the error queue —
        // the idempotency path below absorbed the duplicate cleanly, it did not merely avoid
        // crashing.
        Assert.Empty(trackedSession.MovedToErrorQueue.MessagesOf<BookingHoldExpired>());

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var claims = await db.SentAbandonedCheckoutEmails.AsNoTracking()
                .Where(e => e.BookingId == bookingId).ToListAsync(TestContext.Current.CancellationToken);
            // Exactly one claim row survives the duplicate delivery — SentAbandonedCheckoutEmail's
            // own unique index on BookingId (ADR-0029 Addendum A) is what makes this true, not an
            // assumption.
            Assert.Single(claims);
        }
    }
}
