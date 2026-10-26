using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Notifications.Domain;
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
    // Simulates a confirmed provider rejection (e.g. the 4xx case PostmarkEmailSender classifies
    // as ConfirmedNotSent) — not the ambiguous case; see AmbiguousEmailSender below for that.
    private sealed class AlwaysFailingEmailSender : IEmailSender
    {
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct) =>
            Task.FromResult(EmailSendResult.ConfirmedNotSent("simulated provider outage"));
    }

    // Simulates PostmarkEmailSender's Ambiguous outcome (a transport failure or 5xx response) —
    // the provider may have already accepted the email; this must never be recorded as a skip.
    private sealed class AmbiguousEmailSender : IEmailSender
    {
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct) =>
            Task.FromResult(EmailSendResult.Ambiguous("simulated transport failure"));
    }

    // Throws the exact exception type directly observed twice during this project's own test
    // runs (a transient Npgsql connection failure at this call site) — proves the handler's new
    // technical-failure boundary, not a business-skip return value.
    private sealed class ThrowingUserEmailQuery : IUserEmailQuery
    {
        public Task<string?> GetEmailAsync(Guid userId, CancellationToken ct) =>
            throw new Npgsql.NpgsqlException("Simulated transient connection failure.");
    }

    // Proves the exception-handling boundary's scope covers the email-sender call site too, not
    // just the two cross-module queries — both current IEmailSender implementations
    // (PostmarkEmailSender, TestEmailSender) are non-throwing by construction today, so this is a
    // structural boundary-coverage test, not a claim that Postmark itself throws NpgsqlException.
    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct) =>
            throw new Npgsql.NpgsqlException("Simulated transient connection failure.");
    }

    private sealed class CancellingUserEmailQuery : IUserEmailQuery
    {
        public Task<string?> GetEmailAsync(Guid userId, CancellationToken ct) =>
            throw new OperationCanceledException("Simulated host shutdown/cancellation.");
    }

    // Simulates cancellation surfacing from inside the provider call itself (a real
    // HttpClient-internal timeout or the caller's token firing mid-request) rather than from a
    // pre-send cross-module query.
    private sealed class CancellingEmailSender : IEmailSender
    {
        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct) =>
            throw new OperationCanceledException("Simulated cancellation during the provider call.");
    }

    // Records every Log call verbatim — the narrowest seam to assert the pre-claim failure's
    // distinct log message below without a broad logging abstraction; NullLogger (used by every
    // other test in this file) can't be inspected after the fact.
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    // The exact shape actually observed in this project's own captured failures (Batch 5/6):
    // EF Core's execution strategy wraps a transient Npgsql failure in an InvalidOperationException
    // ("likely due to a transient failure"), with the NpgsqlException as its immediate
    // InnerException — never a bare NpgsqlException at the top. ThrowingUserEmailQuery above only
    // exercises the production helper's "ex is NpgsqlException" branch; this fake exists
    // specifically to exercise the "ex.InnerException is NpgsqlException" branch instead.
    private sealed class WrappedTransientFailureUserEmailQuery : IUserEmailQuery
    {
        public Task<string?> GetEmailAsync(Guid userId, CancellationToken ct) =>
            throw new InvalidOperationException(
                "An exception has been raised that is likely due to a transient failure.",
                new Npgsql.NpgsqlException("Simulated transient connection failure."));
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
                IntroVideoReference: "https://example.com/intro.mp4",
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
    public async Task HandleAsync_TransientNpgsqlFailureFromUserEmailQuery_RecordsTechnicalFailureSkip_NotStranded()
    {
        var (tutorUserId, _, _) = await SeedTutorAsync("Transient Failure Tutor");
        var studentUserId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var tutorProfileQuery = scope.ServiceProvider.GetRequiredService<ITutorProfileSummaryQuery>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<NotificationsOptions>>();

        var handler = new SendAbandonedCheckoutRecoveryEmailFeature.Handler(
            db, TimeProvider.System, emailSender, new ThrowingUserEmailQuery(), tutorProfileQuery, options,
            NullLogger<SendAbandonedCheckoutRecoveryEmailFeature.Handler>.Instance);

        // Must not throw — the handler recovers from this exception type itself.
        await handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, now.AddDays(1), now), TestContext.Current.CancellationToken);

        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claim.WasSent);
        Assert.Equal("Technical failure during processing.", claim.SkippedReason);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.DoesNotContain(sender.SentMessages, m => m.Subject.Contains("Transient Failure Tutor"));
    }

    [Fact]
    public async Task HandleAsync_TransientNpgsqlFailureWrappedInInvalidOperationException_RecordsTechnicalFailureSkip_NotStranded()
    {
        var (tutorUserId, _, _) = await SeedTutorAsync("Wrapped Transient Failure Tutor");
        var studentUserId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var tutorProfileQuery = scope.ServiceProvider.GetRequiredService<ITutorProfileSummaryQuery>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<NotificationsOptions>>();

        var handler = new SendAbandonedCheckoutRecoveryEmailFeature.Handler(
            db, TimeProvider.System, emailSender, new WrappedTransientFailureUserEmailQuery(), tutorProfileQuery, options,
            NullLogger<SendAbandonedCheckoutRecoveryEmailFeature.Handler>.Instance);

        // Must not throw — the handler must recover via the InnerException branch specifically,
        // since the top-level exception here is InvalidOperationException, not NpgsqlException.
        await handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, now.AddDays(1), now), TestContext.Current.CancellationToken);

        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claim.WasSent);
        Assert.Equal("Technical failure during processing.", claim.SkippedReason);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.DoesNotContain(sender.SentMessages, m => m.Subject.Contains("Wrapped Transient Failure Tutor"));
    }

    // Batch 53: once IEmailSender.SendAsync has been invoked, the handler can no longer tell
    // whether the provider accepted the request before this exception surfaced — recording a
    // confident "Technical failure during processing." skip here (the old behavior this test used
    // to assert) could permanently hide a delivery that actually happened. The handler must
    // instead propagate and leave the claim unresolved, matching the existing "stay unresolved
    // when genuinely uncertain" treatment the claim-INSERT path already uses.
    [Fact]
    public async Task HandleAsync_ProviderSendThrowsWithUncertainOutcome_PropagatesAndLeavesClaimUnresolved_NeverRecordsAFalseSkip()
    {
        var (studentUserId, studentEmail) = await SeedStudentAsync("student-transient-sender@example.com");
        var (tutorUserId, _, _) = await SeedTutorAsync("Transient Sender Tutor");
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var userEmailQuery = scope.ServiceProvider.GetRequiredService<IUserEmailQuery>();
        var tutorProfileQuery = scope.ServiceProvider.GetRequiredService<ITutorProfileSummaryQuery>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<NotificationsOptions>>();

        var handler = new SendAbandonedCheckoutRecoveryEmailFeature.Handler(
            db, TimeProvider.System, new ThrowingEmailSender(), userEmailQuery, tutorProfileQuery, options,
            NullLogger<SendAbandonedCheckoutRecoveryEmailFeature.Handler>.Instance);

        await Assert.ThrowsAsync<Npgsql.NpgsqlException>(() =>
            handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, now.AddDays(1), now), TestContext.Current.CancellationToken));

        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claim.WasSent);
        Assert.Null(claim.SkippedReason);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.DoesNotContain(sender.SentMessages, m => m.ToEmail == studentEmail);
    }

    [Fact]
    public async Task HandleAsync_CancellationDuringProcessing_PropagatesCancellation_NeverRecordedAsABusinessOrTechnicalSkip()
    {
        var (tutorUserId, _, _) = await SeedTutorAsync("Cancellation Tutor");
        var studentUserId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var tutorProfileQuery = scope.ServiceProvider.GetRequiredService<ITutorProfileSummaryQuery>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<NotificationsOptions>>();

        var handler = new SendAbandonedCheckoutRecoveryEmailFeature.Handler(
            db, TimeProvider.System, emailSender, new CancellingUserEmailQuery(), tutorProfileQuery, options,
            NullLogger<SendAbandonedCheckoutRecoveryEmailFeature.Handler>.Instance);

        // Cancellation must propagate, not be swallowed/reclassified as any kind of skip — this
        // is a documented residual limitation (process/cancellation can still leave a claim
        // unresolved), not something this batch claims to fix.
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, now.AddDays(1), now), TestContext.Current.CancellationToken));

        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claim.WasSent);
        Assert.Null(claim.SkippedReason);
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

    [Fact]
    public async Task HandleAsync_PreClaimIdempotencyQueryFails_PropagatesAndLogsDistinctly_CreatesNoClaim()
    {
        var tutorUserId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var scope = fixture.Services.CreateScope();
        var tutorProfileQuery = scope.ServiceProvider.GetRequiredService<ITutorProfileSummaryQuery>();
        var userEmailQuery = scope.ServiceProvider.GetRequiredService<IUserEmailQuery>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<NotificationsOptions>>();

        // Points at the same real Testcontainers-backed Postgres server but a database that does
        // not exist — a real, deterministic Npgsql/Postgres failure at the handler's very first
        // database operation (the pre-claim AnyAsync check), without introducing a repository
        // abstraction over NotificationsDbContext just to make this one call injectable. Fast and
        // environment-independent (fails on database selection, not on network routing/timeout).
        var realConnectionString = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.GetConnectionString()!;
        var unreachableConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(realConnectionString)
        {
            Database = "this_database_does_not_exist"
        }.ConnectionString;
        var brokenDbOptions = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(unreachableConnectionString)
            .Options;
        await using var brokenDb = new NotificationsDbContext(brokenDbOptions);

        var capturingLogger = new CapturingLogger<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        var handler = new SendAbandonedCheckoutRecoveryEmailFeature.Handler(
            brokenDb, TimeProvider.System, emailSender, userEmailQuery, tutorProfileQuery, options, capturingLogger);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, now.AddDays(1), now), TestContext.Current.CancellationToken));

        Assert.Contains(capturingLogger.Entries, e =>
            e.Level == LogLevel.Error &&
            e.Message.Contains("pre-claim idempotency check failed", StringComparison.Ordinal) &&
            e.Message.Contains(bookingId.ToString(), StringComparison.Ordinal));

        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claimExists = await db.SentAbandonedCheckoutEmails.AsNoTracking().AnyAsync(
            e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claimExists);
    }

    // --- ADR-0029 Addendum A: unresolved-claim ownership, recovery, and reconciliation ---

    private async Task SeedClaimAsync(Guid bookingId, Guid studentUserId, Guid tutorUserId, DateTimeOffset startUtc, Guid ownerToken, DateTimeOffset ownedUntilUtc)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claim = SentAbandonedCheckoutEmail.Claim(bookingId, studentUserId, tutorUserId, startUtc, ownerToken, TimeSpan.FromMinutes(10), TimeProvider.System);
        db.Add(claim);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Force the lease into whatever state this test needs — bypassing Claim()'s normal
        // "now + configured lease" computation, which this seam deliberately needs full control
        // over (a genuinely old or genuinely fresh lease, not whatever "now" happens to be).
        await db.SentAbandonedCheckoutEmails.Where(e => e.BookingId == bookingId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.OwnedUntilUtc, ownedUntilUtc), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ClaimInsertFailsWithNonUniqueViolation_PropagatesAndLogsDistinctly_NeverFalselyResolves()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var scope = fixture.Services.CreateScope();
        var realDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var tutorProfileQuery = scope.ServiceProvider.GetRequiredService<ITutorProfileSummaryQuery>();
        var userEmailQuery = scope.ServiceProvider.GetRequiredService<IUserEmailQuery>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<NotificationsOptions>>();

        // A real connection to the same Postgres server, forced into a read-only transaction via
        // a genuine server-side option — the pre-claim SELECT succeeds normally (reads are
        // allowed), but the claim INSERT fails with a real Postgres "cannot execute INSERT in a
        // read-only transaction" error (SqlState 25006) — deterministically NOT a unique
        // violation. No mocking/abstraction over NotificationsDbContext is introduced.
        var realConnectionString = realDb.Database.GetConnectionString()!;
        var readOnlyConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(realConnectionString)
        {
            Options = "-c default_transaction_read_only=on"
        }.ConnectionString;
        var readOnlyDbOptions = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(readOnlyConnectionString)
            .Options;
        await using var readOnlyDb = new NotificationsDbContext(readOnlyDbOptions);

        var capturingLogger = new CapturingLogger<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        var handler = new SendAbandonedCheckoutRecoveryEmailFeature.Handler(
            readOnlyDb, TimeProvider.System, emailSender, userEmailQuery, tutorProfileQuery, options, capturingLogger);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, now.AddDays(1), now), TestContext.Current.CancellationToken));

        Assert.Contains(capturingLogger.Entries, e =>
            e.Level == LogLevel.Error &&
            e.Message.Contains("claim persistence failed", StringComparison.Ordinal) &&
            e.Message.Contains(bookingId.ToString(), StringComparison.Ordinal));

        // Never falsely resolved — and since Postgres rejected the write before applying it, no
        // row exists at all against the real database.
        var claimExists = await realDb.SentAbandonedCheckoutEmails.AsNoTracking().AnyAsync(
            e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claimExists);
    }

    [Fact]
    public async Task ReconcileOnceAsync_OldUnresolvedClaim_IsTakenOverAndResolved_EmailSent()
    {
        var (studentUserId, studentEmail) = await SeedStudentAsync("student-stale-claim@example.com");
        var (tutorUserId, displayName, _) = await SeedTutorAsync("Stale Claim Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var originalOwnerToken = Guid.NewGuid();

        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, originalOwnerToken, DateTimeOffset.UtcNow.AddHours(-1));

        using var scope = fixture.Services.CreateScope();
        var reconciliation = scope.ServiceProvider.GetRequiredService<AbandonedCheckoutClaimReconciliationService>();
        await reconciliation.ReconcileOnceAsync(TestContext.Current.CancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.True(claim.WasSent);
        Assert.NotEqual(originalOwnerToken, claim.OwnerToken);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        var sent = Assert.Single(sender.SentMessages, m => m.ToEmail == studentEmail);
        Assert.Contains(displayName, sent.Subject);
    }

    [Fact]
    public async Task ReconcileOnceAsync_RecentUnresolvedClaim_IsNotTakenOverPrematurely()
    {
        var studentUserId = Guid.NewGuid();
        var (tutorUserId, _, _) = await SeedTutorAsync("Fresh Claim Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var originalOwnerToken = Guid.NewGuid();

        // Leased 10 minutes into the future — still well within an active worker's realistic
        // processing window, must not be touched.
        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, originalOwnerToken, DateTimeOffset.UtcNow.AddMinutes(10));

        using var scope = fixture.Services.CreateScope();
        var reconciliation = scope.ServiceProvider.GetRequiredService<AbandonedCheckoutClaimReconciliationService>();
        await reconciliation.ReconcileOnceAsync(TestContext.Current.CancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claim.WasSent);
        Assert.Null(claim.SkippedReason);
        Assert.Equal(originalOwnerToken, claim.OwnerToken);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.DoesNotContain(sender.SentMessages, m => m.Subject.Contains("Fresh Claim Tutor"));
    }

    [Fact]
    public async Task ReconcileOnceAsync_TwoConcurrentAttempts_OnlyOneTakesOverAndSendsExactlyOnce()
    {
        var (studentUserId, studentEmail) = await SeedStudentAsync("student-concurrent-reconcile@example.com");
        var (tutorUserId, _, _) = await SeedTutorAsync("Concurrent Reconcile Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1));

        // Same resolvable singleton — the actual race happens in Postgres via two independent
        // DI scopes ReconcileOnceAsync creates internally per call, exactly like
        // ConcurrencyTests.cs's own real-concurrency pattern for Bookings.
        var reconciliation = fixture.Services.GetRequiredService<AbandonedCheckoutClaimReconciliationService>();

        await Task.WhenAll(
            reconciliation.ReconcileOnceAsync(TestContext.Current.CancellationToken),
            reconciliation.ReconcileOnceAsync(TestContext.Current.CancellationToken));

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.Single(sender.SentMessages, m => m.ToEmail == studentEmail);

        using var verifyScope = fixture.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claimCount = await db.SentAbandonedCheckoutEmails.CountAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.Equal(1, claimCount);
    }

    [Fact]
    public async Task ProcessOwnedClaimAsync_OwnershipLostBeforeSend_AbortsWithoutSending()
    {
        var (studentUserId, studentEmail) = await SeedStudentAsync("student-lost-ownership@example.com");
        var (tutorUserId, _, _) = await SeedTutorAsync("Lost Ownership Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var originalOwnerToken = Guid.NewGuid();

        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, originalOwnerToken, DateTimeOffset.UtcNow.AddMinutes(10));

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        // Simulates a reconciliation takeover racing in between claim and send — stamps a
        // different owner token directly, exactly as the real takeover write would.
        await db.SentAbandonedCheckoutEmails.Where(e => e.BookingId == bookingId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.OwnerToken, Guid.NewGuid()), TestContext.Current.CancellationToken);

        var tutorProfileQuery = scope.ServiceProvider.GetRequiredService<ITutorProfileSummaryQuery>();
        var userEmailQuery = scope.ServiceProvider.GetRequiredService<IUserEmailQuery>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<NotificationsOptions>>();
        var capturingLogger = new CapturingLogger<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        var handler = new SendAbandonedCheckoutRecoveryEmailFeature.Handler(
            db, TimeProvider.System, emailSender, userEmailQuery, tutorProfileQuery, options, capturingLogger);

        // Calling with the ORIGINAL (now-stale) token must not throw and must not send.
        await handler.ProcessOwnedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, originalOwnerToken, TestContext.Current.CancellationToken);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.DoesNotContain(sender.SentMessages, m => m.ToEmail == studentEmail);
        Assert.Contains(capturingLogger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("lost claim ownership", StringComparison.Ordinal));
    }

    // --- Batch 51: deliberate duplicate-event behavior (ADR-0029 Addendum A, Batch 51 update) ---

    [Fact]
    public async Task HandleAsync_DuplicateEventAgainstResolvedClaim_NoAdditionalEmail_NoStateChange()
    {
        var (studentUserId, studentEmail) = await SeedStudentAsync("student-dup-resolved@example.com");
        var (tutorUserId, _, _) = await SeedTutorAsync("Dup Resolved Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var ownerToken = Guid.NewGuid();
        var sentAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30);

        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, ownerToken, DateTimeOffset.UtcNow.AddMinutes(10));

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var resolved = await db.SentAbandonedCheckoutEmails.Where(e => e.BookingId == bookingId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.WasSent, true).SetProperty(e => e.SentAtUtc, sentAtUtc), TestContext.Current.CancellationToken);
            Assert.Equal(1, resolved);
        }

        using var handlerScope = fixture.Services.CreateScope();
        var handler = handlerScope.ServiceProvider.GetRequiredService<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        await handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, startUtc, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.DoesNotContain(sender.SentMessages, m => m.ToEmail == studentEmail);

        using var verifyScope = fixture.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claim = await verifyDb.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.True(claim.WasSent);
        Assert.Equal(ownerToken, claim.OwnerToken);
        // Not exact equality — Postgres "timestamp with time zone" stores microsecond precision,
        // one digit coarser than a .NET DateTimeOffset tick, so an exact round-trip comparison is
        // not meaningful here. The invariant under test is "untouched by the duplicate event," not
        // sub-microsecond timestamp fidelity.
        Assert.True((sentAtUtc - claim.SentAtUtc!.Value).Duration() < TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task HandleAsync_DuplicateEventAgainstRecentUnresolvedClaim_NoPrematureTakeover_NoEmail()
    {
        var studentUserId = Guid.NewGuid();
        var (tutorUserId, _, _) = await SeedTutorAsync("Dup Recent Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var originalOwnerToken = Guid.NewGuid();

        // Owned by another worker, still well within its lease — the duplicate delivery must not
        // steal it.
        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, originalOwnerToken, DateTimeOffset.UtcNow.AddMinutes(10));

        using var handlerScope = fixture.Services.CreateScope();
        var handler = handlerScope.ServiceProvider.GetRequiredService<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        await handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, startUtc, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        using var verifyScope = fixture.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claim.WasSent);
        Assert.Null(claim.SkippedReason);
        Assert.Equal(originalOwnerToken, claim.OwnerToken);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.DoesNotContain(sender.SentMessages, m => m.Subject.Contains("Dup Recent Tutor"));
    }

    [Fact]
    public async Task HandleAsync_DuplicateEventAgainstStaleUnresolvedClaim_TakesOverViaEventPath_EmailSentExactlyOnce()
    {
        var (studentUserId, studentEmail) = await SeedStudentAsync("student-dup-stale@example.com");
        var (tutorUserId, displayName, _) = await SeedTutorAsync("Dup Stale Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var originalOwnerToken = Guid.NewGuid();

        // Abandoned (lease long expired) — the duplicate delivery is entitled to recover this
        // directly, through the same atomic staleness-gated takeover reconciliation uses.
        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, originalOwnerToken, DateTimeOffset.UtcNow.AddHours(-1));

        using var handlerScope = fixture.Services.CreateScope();
        var handler = handlerScope.ServiceProvider.GetRequiredService<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        await handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, startUtc, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        using var verifyScope = fixture.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.True(claim.WasSent);
        Assert.NotEqual(originalOwnerToken, claim.OwnerToken);

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        var sent = Assert.Single(sender.SentMessages, m => m.ToEmail == studentEmail);
        Assert.Contains(displayName, sent.Subject);
    }

    [Fact]
    public async Task HandleAsync_RacingReconciliation_OnlyOneOwnerResolvesTheClaim()
    {
        var (studentUserId, studentEmail) = await SeedStudentAsync("student-handler-vs-reconcile@example.com");
        var (tutorUserId, _, _) = await SeedTutorAsync("Handler Vs Reconcile Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);

        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1));

        using var handlerScope = fixture.Services.CreateScope();
        var handler = handlerScope.ServiceProvider.GetRequiredService<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        var reconciliation = fixture.Services.GetRequiredService<AbandonedCheckoutClaimReconciliationService>();

        // The real event-handler entry point and the real reconciliation entry point, racing for
        // the SAME stale claim at the database level — not a simulation of either side.
        await Task.WhenAll(
            handler.HandleAsync(Event(bookingId, studentUserId, tutorUserId, startUtc, DateTimeOffset.UtcNow), TestContext.Current.CancellationToken),
            reconciliation.ReconcileOnceAsync(TestContext.Current.CancellationToken));

        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        Assert.Single(sender.SentMessages, m => m.ToEmail == studentEmail);

        using var verifyScope = fixture.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var claimCount = await db.SentAbandonedCheckoutEmails.CountAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.Equal(1, claimCount);
        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.True(claim.WasSent);
    }

    [Fact]
    public async Task ReconcileOnceAsync_RespectsConfiguredBatchLimit()
    {
        var smallBatchOptions = Microsoft.Extensions.Options.Options.Create(new NotificationsOptions { ClaimReconciliationBatchSize = 2, ClaimLeaseMinutes = 10 });

        using var scope = fixture.Services.CreateScope();
        var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        var reconciliation = new AbandonedCheckoutClaimReconciliationService(
            scopeFactory, TimeProvider.System, smallBatchOptions, NullLogger<AbandonedCheckoutClaimReconciliationService>.Instance);

        var bookingIds = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            // Intentionally unseeded student — forces a fast, deterministic business skip rather
            // than a real send, keeping this test about the batch count only.
            var studentUserId = Guid.NewGuid();
            var (tutorUserId, _, _) = await SeedTutorAsync($"Batch Tutor {i}");
            var bookingId = Guid.NewGuid();
            await SeedClaimAsync(bookingId, studentUserId, tutorUserId, DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1));
            bookingIds.Add(bookingId);
        }

        await reconciliation.ReconcileOnceAsync(TestContext.Current.CancellationToken);

        using var verifyScope = fixture.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var resolvedCount = await db.SentAbandonedCheckoutEmails
            .CountAsync(e => bookingIds.Contains(e.BookingId) && e.SkippedReason != null, TestContext.Current.CancellationToken);

        Assert.Equal(2, resolvedCount);
    }

    [Fact]
    public async Task ReconcileOnceAsync_AlreadyCancelledToken_ThrowsRatherThanSilentlyCompleting()
    {
        using var scope = fixture.Services.CreateScope();
        var reconciliation = scope.ServiceProvider.GetRequiredService<AbandonedCheckoutClaimReconciliationService>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reconciliation.ReconcileOnceAsync(cts.Token));
    }

    // --- Batch 53: the external-send boundary (ADR-0029 Addendum A, Batch 53 update) ---

    [Fact]
    public async Task ProcessOwnedClaimAsync_ProviderSucceeds_ResolutionWriteFails_LeavesClaimUnresolved_NeverRecordsTheSendAsAFailure()
    {
        var (studentUserId, studentEmail) = await SeedStudentAsync("student-post-send-write-fail@example.com");
        var (tutorUserId, _, _) = await SeedTutorAsync("Post Send Write Fail Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var ownerToken = Guid.NewGuid();

        // A fresh (not stale) claim, owned by this token, seeded via the real writable connection.
        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, ownerToken, DateTimeOffset.UtcNow.AddMinutes(10));

        using var scope = fixture.Services.CreateScope();
        var realDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var tutorProfileQuery = scope.ServiceProvider.GetRequiredService<ITutorProfileSummaryQuery>();
        var userEmailQuery = scope.ServiceProvider.GetRequiredService<IUserEmailQuery>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<NotificationsOptions>>();

        // A real connection to the same Postgres server, forced into a read-only transaction via a
        // genuine server-side option — the pre-send reads and ownership check succeed normally,
        // but the post-send resolve write fails with a real Postgres "cannot execute UPDATE in a
        // read-only transaction" error (SqlState 25006) — deterministically AFTER a real send (via
        // a dedicated TestEmailSender instance, not the fixture's shared one) has already
        // succeeded. No mocking/abstraction over NotificationsDbContext is introduced.
        var realConnectionString = realDb.Database.GetConnectionString()!;
        var readOnlyConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(realConnectionString)
        {
            Options = "-c default_transaction_read_only=on"
        }.ConnectionString;
        var readOnlyDbOptions = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(readOnlyConnectionString)
            .Options;
        await using var readOnlyDb = new NotificationsDbContext(readOnlyDbOptions);

        var dedicatedSender = new TestEmailSender();
        var capturingLogger = new CapturingLogger<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        var handler = new SendAbandonedCheckoutRecoveryEmailFeature.Handler(
            readOnlyDb, TimeProvider.System, dedicatedSender, userEmailQuery, tutorProfileQuery, options, capturingLogger);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.ProcessOwnedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, ownerToken, TestContext.Current.CancellationToken));

        // The email WAS genuinely sent — this is exactly the scenario the old behavior got wrong:
        // it would have recorded "Technical failure during processing." here despite this real
        // send having already succeeded.
        Assert.Single(dedicatedSender.SentMessages, m => m.ToEmail == studentEmail);

        Assert.Contains(capturingLogger.Entries, e =>
            e.Level == LogLevel.Error &&
            e.Message.Contains("accepted by the provider", StringComparison.Ordinal) &&
            e.Message.Contains(bookingId.ToString(), StringComparison.Ordinal));

        // Never a false terminal skip — the claim remains genuinely unresolved, still owned by the
        // same token, exactly as reconciliation expects a stuck-but-possibly-sent claim to look.
        var claim = await realDb.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claim.WasSent);
        Assert.Null(claim.SkippedReason);
        Assert.Equal(ownerToken, claim.OwnerToken);
    }

    [Fact]
    public async Task TryResolveAsOwnerAsync_StaleOwnerToken_CannotOverwriteClaimAfterOwnershipHasMovedOn()
    {
        var (studentUserId, _) = await SeedStudentAsync("student-stale-overwrite@example.com");
        var (tutorUserId, _, _) = await SeedTutorAsync("Stale Overwrite Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var staleOwnerToken = Guid.NewGuid();
        var newOwnerToken = Guid.NewGuid();

        // Simulates the scenario the previous test exercises end-to-end: the original worker's
        // post-send resolve write failed and left the claim unresolved; its lease later expired;
        // a new owner (reconciliation, or a redelivered event) legitimately took over and resolved
        // it as sent. The stale worker must never be able to overwrite that outcome.
        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, staleOwnerToken, DateTimeOffset.UtcNow.AddMinutes(10));

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var sentAtUtc = DateTimeOffset.UtcNow;
        await db.SentAbandonedCheckoutEmails.Where(e => e.BookingId == bookingId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.OwnerToken, newOwnerToken)
                .SetProperty(e => e.WasSent, true)
                .SetProperty(e => e.SentAtUtc, sentAtUtc), TestContext.Current.CancellationToken);

        // The stale worker's own (hypothetical) attempt to finalize using its original token —
        // must be a complete no-op against the real database.
        var affected = await AbandonedCheckoutClaimWrites.TryResolveAsOwnerAsync(
            db, bookingId, staleOwnerToken, wasSent: false, sentAtUtc: null, "Technical failure during processing.", TestContext.Current.CancellationToken);
        Assert.Equal(0, affected);

        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.True(claim.WasSent);
        Assert.Null(claim.SkippedReason);
        Assert.Equal(newOwnerToken, claim.OwnerToken);
    }

    // --- Batch 55: ambiguous provider delivery outcomes (ADR-0029 Addendum A, Batch 55 update) ---

    [Fact]
    public async Task ProcessOwnedClaimAsync_ProviderOutcomeAmbiguous_LeavesClaimUnresolved_NeverRecordsATerminalSkip_AndRemainsEligibleForReconciliation()
    {
        var (studentUserId, studentEmail) = await SeedStudentAsync("student-ambiguous-outcome@example.com");
        var (tutorUserId, displayName, _) = await SeedTutorAsync("Ambiguous Outcome Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var ownerToken = Guid.NewGuid();

        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, ownerToken, DateTimeOffset.UtcNow.AddMinutes(10));

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var tutorProfileQuery = scope.ServiceProvider.GetRequiredService<ITutorProfileSummaryQuery>();
            var userEmailQuery = scope.ServiceProvider.GetRequiredService<IUserEmailQuery>();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<NotificationsOptions>>();
            var capturingLogger = new CapturingLogger<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
            var handler = new SendAbandonedCheckoutRecoveryEmailFeature.Handler(
                db, TimeProvider.System, new AmbiguousEmailSender(), userEmailQuery, tutorProfileQuery, options, capturingLogger);

            // Ambiguity is not an error condition for the caller — must not throw.
            await handler.ProcessOwnedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, ownerToken, TestContext.Current.CancellationToken);

            Assert.Contains(capturingLogger.Entries, e =>
                e.Level == LogLevel.Error &&
                e.Message.Contains("could not be determined", StringComparison.Ordinal) &&
                e.Message.Contains(bookingId.ToString(), StringComparison.Ordinal));
        }

        using (var verifyScope = fixture.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

            // Never a terminal skip — the claim is genuinely unresolved, still owned by the same
            // token (distinguishes "unresolved" from "terminal-skipped", not merely WasSent).
            var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
            Assert.False(claim.WasSent);
            Assert.Null(claim.SkippedReason);
            Assert.Equal(ownerToken, claim.OwnerToken);

            // Age the lease to simulate time passing, proving the claim remains eligible for
            // lease-based reconciliation rather than being silently stuck forever.
            await db.SentAbandonedCheckoutEmails.Where(e => e.BookingId == bookingId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.OwnedUntilUtc, DateTimeOffset.UtcNow.AddHours(-1)), TestContext.Current.CancellationToken);
        }

        using var reconcileScope = fixture.Services.CreateScope();
        var reconciliation = reconcileScope.ServiceProvider.GetRequiredService<AbandonedCheckoutClaimReconciliationService>();
        await reconciliation.ReconcileOnceAsync(TestContext.Current.CancellationToken);

        // Reconciliation (using the fixture's real TestEmailSender, not the ambiguous fake) is
        // able to take over and genuinely resolve the claim — this is the accepted duplicate-send
        // trade-off documented in ADR-0029: if the earlier ambiguous attempt had actually been
        // accepted by the provider, this is a second, real send.
        var sender = (TestEmailSender)fixture.Services.GetRequiredService<IEmailSender>();
        var sent = Assert.Single(sender.SentMessages, m => m.ToEmail == studentEmail);
        Assert.Contains(displayName, sent.Subject);

        using var finalScope = fixture.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var finalClaim = await finalDb.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.True(finalClaim.WasSent);
        Assert.NotEqual(ownerToken, finalClaim.OwnerToken);
    }

    [Fact]
    public async Task ProcessOwnedClaimAsync_CancellationDuringSendAttempt_PropagatesCancellation_NeverRecordedAsASkip()
    {
        var (studentUserId, _) = await SeedStudentAsync("student-cancel-during-send@example.com");
        var (tutorUserId, _, _) = await SeedTutorAsync("Cancel During Send Tutor");
        var bookingId = Guid.NewGuid();
        var startUtc = DateTimeOffset.UtcNow.AddDays(1);
        var ownerToken = Guid.NewGuid();

        await SeedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, ownerToken, DateTimeOffset.UtcNow.AddMinutes(10));

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var tutorProfileQuery = scope.ServiceProvider.GetRequiredService<ITutorProfileSummaryQuery>();
        var userEmailQuery = scope.ServiceProvider.GetRequiredService<IUserEmailQuery>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<NotificationsOptions>>();

        var handler = new SendAbandonedCheckoutRecoveryEmailFeature.Handler(
            db, TimeProvider.System, new CancellingEmailSender(), userEmailQuery, tutorProfileQuery, options,
            NullLogger<SendAbandonedCheckoutRecoveryEmailFeature.Handler>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            handler.ProcessOwnedClaimAsync(bookingId, studentUserId, tutorUserId, startUtc, ownerToken, TestContext.Current.CancellationToken));

        var claim = await db.SentAbandonedCheckoutEmails.AsNoTracking().FirstAsync(e => e.BookingId == bookingId, TestContext.Current.CancellationToken);
        Assert.False(claim.WasSent);
        Assert.Null(claim.SkippedReason);
        Assert.Equal(ownerToken, claim.OwnerToken);
    }
}
