using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Modules.Notifications.Persistence;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using SendAbandonedCheckoutRecoveryEmailFeature = TutorFlow.Modules.Notifications.Features.SendAbandonedCheckoutRecoveryEmail.SendAbandonedCheckoutRecoveryEmail;

namespace TutorFlow.Modules.Notifications.Tests;

/// <summary>
/// ADR-0029: the recovery handler's core correctness — exactly-once sending, correct
/// recipient/content, idempotency under redelivery, and graceful handling of missing data.
/// Seeds real Identity/Tutors rows (not mocks) so <c>IUserEmailQuery</c>/
/// <c>ITutorProfileSummaryQuery</c> resolve genuine cross-module data, matching this codebase's
/// established "real DI wiring, not a mocked substitute" convention.
/// </summary>
public class SendAbandonedCheckoutRecoveryEmailTests(NotificationsApiFixture fixture) : IClassFixture<NotificationsApiFixture>
{
    private sealed class AlwaysFailingEmailSender : IEmailSender
    {
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct) =>
            Task.FromResult(EmailSendResult.Failure("simulated provider outage"));
    }

    private async Task<(Guid StudentUserId, string Email)> SeedStudentAsync(string email)
    {
        using var scope = fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email, CreatedAtUtc = DateTimeOffset.UtcNow };
        var result = await userManager.CreateAsync(user, "Password123!");
        Assert.True(result.Succeeded);
        return (user.Id, email);
    }

    private async Task<(Guid TutorUserId, string DisplayName, string Slug)> SeedTutorAsync(string displayName)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var clock = TimeProvider.System;
        var application = TutorApplication.CreateDraft(Guid.NewGuid(), clock);
        application.ApplyDraftUpdate(
            new DraftUpdate(
                DisplayName: displayName,
                Headline: "Patient tutor",
                ProfilePhotoReference: "https://example.com/photo.jpg",
                SubjectIds: [Guid.NewGuid()],
                SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                HourlyRate: new Money(2500, "EUR")),
            clock);
        db.TutorApplications.Add(application);
        application.Submit(clock);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (application.UserId, displayName, application.PublicSlug!);
    }

    private static BookingHoldExpired Event(Guid bookingId, Guid studentUserId, Guid tutorUserId, DateTimeOffset startUtc, DateTimeOffset now) =>
        new(bookingId, studentUserId, tutorUserId, startUtc, startUtc.AddMinutes(25), 25, true, now);

    [Fact]
    public async Task HandleAsync_QualifyingEvent_SendsExactlyOneEmail_ToTheStudentWithCorrectTutorAndSlotData()
    {
        var (studentUserId, email) = await SeedStudentAsync("student-correctness@example.com");
        var (tutorUserId, displayName, slug) = await SeedTutorAsync("Ada Lovelace");
        var bookingId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 10, 6, 14, 0, 0, TimeSpan.Zero);
        var startUtc = new DateTimeOffset(2026, 10, 6, 13, 30, 0, TimeSpan.Zero);

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        await handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, startUtc, now), TestContext.Current.CancellationToken);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        var sent = Assert.Single(sender.SentMessages, m => m.ToEmail == email);
        Assert.Contains(displayName, sent.Subject);
        Assert.Contains(displayName, sent.HtmlBody);
        Assert.Contains("13:30", sent.HtmlBody);
        Assert.Contains($"/tutors/{slug}", sent.HtmlBody);

        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.True(claim.WasSent);
        Assert.NotNull(claim.SentAtUtc);
    }

    [Fact]
    public async Task HandleAsync_CalledTwiceForTheSameBooking_SendsOnlyOneEmail_NotADuplicate()
    {
        var (studentUserId, email) = await SeedStudentAsync("student-dup@example.com");
        var (tutorUserId, _, _) = await SeedTutorAsync("Dup Tutor");
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var evt = Event(bookingId, studentUserId, tutorUserId, now.AddDays(1), now);

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();

        // Simulates a redelivered message / a retried handler invocation.
        await handler.HandleAsync(evt, TestContext.Current.CancellationToken);
        await handler.HandleAsync(evt, TestContext.Current.CancellationToken);
        await handler.HandleAsync(evt, TestContext.Current.CancellationToken);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.Single(sender.SentMessages, m => m.ToEmail == email);

        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claimCount = await db.SentAbandonedCheckoutEmails.CountAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.Equal(1, claimCount);
    }

    [Fact]
    public async Task HandleAsync_StudentHasNoEmailOnFile_SkipsSafely_NoCrash_NoEmailSent()
    {
        var studentUserId = Guid.NewGuid(); // never seeded — no Identity row exists for this id
        var (tutorUserId, _, _) = await SeedTutorAsync("Ghost Student Tutor");
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();

        // Must not throw.
        await handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, now.AddDays(1), now), TestContext.Current.CancellationToken);

        // The fixture's TestEmailSender is a shared singleton across every test in this class
        // (so it can be inspected after a real DI-resolved call) — assertions must therefore
        // scope to this test's own unique tutor/booking rather than assume an empty bag.
        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.DoesNotContain(sender.SentMessages, m => m.Subject.Contains("Ghost Student Tutor"));

        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claim.WasSent);
        Assert.Equal("No email on file for this student.", claim.SkippedReason);
    }

    [Fact]
    public async Task HandleAsync_TutorProfileUnavailable_SkipsSafely_NoCrash_NoEmailSent()
    {
        var (studentUserId, studentEmail) = await SeedStudentAsync("student-ghost-tutor@example.com");
        var tutorUserId = Guid.NewGuid(); // never seeded — no Tutors row exists for this id
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();

        await handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, now.AddDays(1), now), TestContext.Current.CancellationToken);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.DoesNotContain(sender.SentMessages, m => m.ToEmail == studentEmail);
    }

    [Fact]
    public async Task HandleAsync_ProviderSendFails_RecordsSkipped_NeverLogsOrPersistsRawProviderError()
    {
        var (studentUserId, _) = await SeedStudentAsync("student-provider-fail@example.com");
        var (tutorUserId, _, _) = await SeedTutorAsync("Provider Fail Tutor");
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var userEmailQuery = scope.ServiceProvider.GetRequiredService<IUserEmailQuery>();
        var tutorProfileQuery = scope.ServiceProvider.GetRequiredService<ITutorProfileSummaryQuery>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<NotificationsOptions>>();

        // Direct construction with a failing sender — the only way to deterministically force
        // the provider-failure path without a real (or Testcontainers-backed) outage.
        var handler = new SendAbandonedCheckoutRecoveryEmailFeature.Handler(
            db, TimeProvider.System, new AlwaysFailingEmailSender(), userEmailQuery, tutorProfileQuery, options, NullLogger<SendAbandonedCheckoutRecoveryEmailFeature.Handler>.Instance);

        await handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, now.AddDays(1), now), TestContext.Current.CancellationToken);

        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claim.WasSent);
        // Never the raw provider error text (which could echo back request fields) — only the
        // fixed, non-PII-bearing classification.
        Assert.Equal("Provider send failed.", claim.SkippedReason);
        Assert.DoesNotContain("simulated provider outage", claim.SkippedReason);
    }

    [Fact]
    public async Task HandleAsync_NeverSendsToTheTutor_OnlyToTheStudentResolvedFromTheEvent()
    {
        var (studentUserId, studentEmail) = await SeedStudentAsync("ownership-student@example.com");
        var (tutorUserId, _, _) = await SeedTutorAsync("Ownership Tutor");
        // The tutor also happens to have an Identity account with its own email — confirms the
        // handler never resolves or sends to it; only StudentUserId's email is ever used.
        await SeedTutorsOwnIdentityAccountAsync(tutorUserId, "ownership-tutor@example.com");
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        await handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, now.AddDays(1), now), TestContext.Current.CancellationToken);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        var relevant = sender.SentMessages.Where(m => m.ToEmail == studentEmail || m.ToEmail == "ownership-tutor@example.com").ToList();
        Assert.Single(relevant);
        Assert.Equal(studentEmail, relevant[0].ToEmail);
    }

    private async Task SeedTutorsOwnIdentityAccountAsync(Guid tutorUserId, string email)
    {
        using var scope = fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = tutorUserId, UserName = email, Email = email, CreatedAtUtc = DateTimeOffset.UtcNow };
        var result = await userManager.CreateAsync(user, "Password123!");
        Assert.True(result.Succeeded);
    }
}
