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

namespace TutorFlow.Host.Tests;

/// <summary>
/// ADR-0029: the full real pipeline — Bookings' sweep publishes <c>BookingHoldExpired</c> via
/// Wolverine, Notifications' real consumer receives it and attempts a real send — proven
/// end-to-end, no mocked cross-module boundary.
/// </summary>
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

        using (var scope = fixture.Services.CreateScope())
        {
            var sweepService = scope.ServiceProvider.GetRequiredService<BookingHoldExpirySweepService>();
            await sweepService.SweepOnceAsync(TestContext.Current.CancellationToken);
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var bookingsDb = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var status = await bookingsDb.Bookings.AsNoTracking().Where(b => b.Id == bookingId).Select(b => b.Status).SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(BookingStatus.Expired, status);
        }

        // Wolverine's local in-process transport delivers asynchronously — the publish call
        // above already returned; the consumer runs in the background.
        var claim = await PollForClaimAsync(bookingId);
        Assert.NotNull(claim);
        Assert.True(claim.WasSent);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        var sent = Assert.Single(sender.SentMessages, m => m.ToEmail == studentEmail);
        Assert.Contains(tutorDisplayName, sent.Subject);
    }

    private async Task<SentAbandonedCheckoutEmail?> PollForClaimAsync(Guid bookingId)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            using var scope = fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstOrDefaultAsync(
                e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
            if (claim is { WasSent: true })
            {
                return claim;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        return null;
    }
}
