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
}
