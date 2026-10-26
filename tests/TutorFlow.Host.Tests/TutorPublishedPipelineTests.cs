using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Infrastructure;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Search.Domain;
using TutorFlow.Modules.Search.Persistence;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using PublishApplicationFeature = TutorFlow.Modules.Tutors.Features.PublishApplication.PublishApplication;
using ReplaceWeeklyRulesFeature = TutorFlow.Modules.Availability.Features.ReplaceWeeklyRules.ReplaceWeeklyRules;
using SuspendApplicationFeature = TutorFlow.Modules.Tutors.Features.SuspendApplication.SuspendApplication;
using AvailabilityTutorPublishedConsumer = TutorFlow.Modules.Availability.Consumers.TutorPublishedConsumer;

namespace TutorFlow.Host.Tests;

/// <summary>
/// Proves the cross-module event pipelines this repo's Wolverine wiring is responsible for
/// (ADR-0021, extended by ADR-0024): Tutors publishes <c>TutorPublished</c>/<c>TutorUnpublished</c>
/// via Wolverine, and both Search and Availability react — with no direct DbContext/project
/// reference between any of the three modules.
/// </summary>
[Collection(HostCollection.Name)]
public sealed class TutorPublishedPipelineTests(TutorPublishedPipelineFixture fixture) : IClassFixture<TutorPublishedPipelineFixture>
{
    [Fact]
    public async Task PublishingATutorApplication_UpsertsASearchDocument_ViaWolverine()
    {
        var userId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        Guid applicationId;

        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var clock = TimeProvider.System;

            var application = TutorApplication.CreateDraft(userId, clock);
            application.ApplyDraftUpdate(
                new DraftUpdate(
                    DisplayName: "Pipeline Test Tutor",
                    Headline: "Patient and encouraging",
                    ProfilePhotoReference: "https://example.com/photo.jpg",
                    IntroVideoReference: "https://example.com/intro.mp4",
                    SubjectIds: [subjectId],
                    SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                    HourlyRate: new Money(3000, "EUR")),
                clock);
            db.TutorApplications.Add(application);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            // No HTTP endpoint exists for Submit->BeginReview->Approve->Publish's admin-gated
            // middle steps (blocked on Identity's missing Admin role, A35) — direct domain-method
            // calls here are the test-only equivalent of TestTutorSetup.cs's existing pattern.
            application.Submit(clock);
            application.BeginReview(clock);
            application.Approve(clock);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            applicationId = application.Id;
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<PublishApplicationFeature.Handler>();
            var result = await handler.HandleAsync(applicationId, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess);
        }

        var document = await PollForSearchDocumentAsync(userId);

        Assert.NotNull(document);
        Assert.Equal("Pipeline Test Tutor", document.DisplayName);
        Assert.Equal("Patient and encouraging", document.Headline);
        Assert.Contains(subjectId, document.SubjectIds);
        Assert.Contains("en", document.NativeLanguageCodes);
        Assert.Equal(3000, document.HourlyRateMinorUnits);
        Assert.True(document.IsPublished);
        Assert.True(document.IsAcceptingNewStudents);
    }

    [Fact]
    public async Task PublishingATutorApplication_WithConfiguredWeeklyAvailability_MakesItBookable_AndSetsARealAvailabilitySignal_ViaWolverine()
    {
        var userId = Guid.NewGuid();
        Guid applicationId;

        using (var scope = fixture.Services.CreateScope())
        {
            var tutorsDb = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var clock = TimeProvider.System;

            var application = TutorApplication.CreateDraft(userId, clock);
            application.ApplyDraftUpdate(
                new DraftUpdate(
                    DisplayName: "Bookable Pipeline Tutor",
                    Headline: "Always has slots",
                    ProfilePhotoReference: "https://example.com/photo.jpg",
                    IntroVideoReference: "https://example.com/intro.mp4",
                    SubjectIds: [Guid.NewGuid()],
                    SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                    HourlyRate: new Money(2500, "EUR")),
                clock);
            tutorsDb.TutorApplications.Add(application);
            application.Submit(clock);
            application.BeginReview(clock);
            application.Approve(clock);
            await tutorsDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            applicationId = application.Id;

            // Every day of the week, nearly the full day — guarantees an open slot exists
            // "soon" regardless of which real-world day this test happens to run on, with no
            // dependency on the current date/time beyond "some hour of some day."
            var availabilityDb = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
            var settings = BookingSettings.Create(userId, "UTC", minNoticeHours: 0, maxBookingWindowDays: 7, defaultSlotDurationMinutes: 30, clock);
            Assert.True(settings.IsSuccess);
            availabilityDb.BookingSettings.Add(settings.Value);

            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                var rule = WeeklyAvailabilityRule.Create(userId, day, new TimeOnly(0, 0), new TimeOnly(23, 30));
                Assert.True(rule.IsSuccess);
                availabilityDb.WeeklyAvailabilityRules.Add(rule.Value);
            }

            await availabilityDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<PublishApplicationFeature.Handler>();
            var result = await handler.HandleAsync(applicationId, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess);
        }

        var document = await PollForConditionAsync(userId, d => d.HasUpcomingAvailability);
        Assert.NotNull(document);
        Assert.True(document.HasUpcomingAvailability);

        using (var scope = fixture.Services.CreateScope())
        {
            var availabilityDb = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
            var bookability = await availabilityDb.TutorBookabilityRecords.FirstOrDefaultAsync(
                b => b.TutorUserId == userId, TestContext.Current.CancellationToken);
            Assert.NotNull(bookability);
            Assert.True(bookability.IsBookable);
        }
    }

    [Fact]
    public async Task UpdatingWeeklyRules_ForAnAlreadyBookableTutor_RepublishesTheAvailabilitySignal_ViaWolverine()
    {
        var userId = Guid.NewGuid();
        Guid applicationId;

        using (var scope = fixture.Services.CreateScope())
        {
            var tutorsDb = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var clock = TimeProvider.System;

            var application = TutorApplication.CreateDraft(userId, clock);
            application.ApplyDraftUpdate(
                new DraftUpdate(
                    DisplayName: "Resync Pipeline Tutor",
                    Headline: "Updates after publish",
                    ProfilePhotoReference: "https://example.com/photo.jpg",
                    IntroVideoReference: "https://example.com/intro.mp4",
                    SubjectIds: [Guid.NewGuid()],
                    SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                    HourlyRate: new Money(2000, "EUR")),
                clock);
            tutorsDb.TutorApplications.Add(application);
            application.Submit(clock);
            application.BeginReview(clock);
            application.Approve(clock);
            await tutorsDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            applicationId = application.Id;

            // No availability configured at publish time — HasUpcomingAvailability starts false.
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<PublishApplicationFeature.Handler>();
            await handler.HandleAsync(applicationId, TestContext.Current.CancellationToken);
        }

        await PollForSearchDocumentAsync(userId);

        // Now configure booking settings/weekly rules via the self-service handlers directly
        // (the real production call path, not a shortcut) and confirm the signal updates
        // without a second publish.
        using (var scope = fixture.Services.CreateScope())
        {
            var availabilityDb = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
            var clock = TimeProvider.System;
            var settings = BookingSettings.Create(userId, "UTC", minNoticeHours: 0, maxBookingWindowDays: 7, defaultSlotDurationMinutes: 30, clock);
            Assert.True(settings.IsSuccess);
            availabilityDb.BookingSettings.Add(settings.Value);
            await availabilityDb.SaveChangesAsync(TestContext.Current.CancellationToken);

            var rules = Enum.GetValues<DayOfWeek>()
                .Select(day => new ReplaceWeeklyRulesFeature.RuleInput(day, new TimeOnly(0, 0), new TimeOnly(23, 30)))
                .ToList();
            var replaceHandler = scope.ServiceProvider.GetRequiredService<ReplaceWeeklyRulesFeature.Handler>();
            var replaceResult = await replaceHandler.HandleAsync(userId, new ReplaceWeeklyRulesFeature.Command(rules), TestContext.Current.CancellationToken);
            Assert.True(replaceResult.IsSuccess);
        }

        var document = await PollForConditionAsync(userId, d => d.HasUpcomingAvailability);
        Assert.NotNull(document);
        Assert.True(document.HasUpcomingAvailability);
    }

    [Fact]
    public async Task SuspendingAPublishedTutor_RemovesItFromSearch_AndClearsBookability_ViaWolverine()
    {
        var userId = Guid.NewGuid();
        Guid applicationId;

        using (var scope = fixture.Services.CreateScope())
        {
            var tutorsDb = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var clock = TimeProvider.System;

            var application = TutorApplication.CreateDraft(userId, clock);
            application.ApplyDraftUpdate(
                new DraftUpdate(
                    DisplayName: "Suspend Pipeline Tutor",
                    Headline: "About to be suspended",
                    ProfilePhotoReference: "https://example.com/photo.jpg",
                    IntroVideoReference: "https://example.com/intro.mp4",
                    SubjectIds: [Guid.NewGuid()],
                    SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                    HourlyRate: new Money(2000, "EUR")),
                clock);
            tutorsDb.TutorApplications.Add(application);
            application.Submit(clock);
            application.BeginReview(clock);
            application.Approve(clock);
            await tutorsDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            applicationId = application.Id;
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var publishHandler = scope.ServiceProvider.GetRequiredService<PublishApplicationFeature.Handler>();
            await publishHandler.HandleAsync(applicationId, TestContext.Current.CancellationToken);
        }

        var published = await PollForSearchDocumentAsync(userId);
        Assert.NotNull(published);
        Assert.True(published.IsPublished);

        using (var scope = fixture.Services.CreateScope())
        {
            var availabilityDb = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
            // Confirm TutorPublished already made this tutor bookable before we prove Suspend
            // reverses it — otherwise a false-negative "not bookable" below would be meaningless.
            for (var attempt = 0; attempt < 50; attempt++)
            {
                var record = await availabilityDb.TutorBookabilityRecords.AsNoTracking().FirstOrDefaultAsync(
                    b => b.TutorUserId == userId, TestContext.Current.CancellationToken);
                if (record is { IsBookable: true })
                {
                    break;
                }

                await Task.Delay(100, TestContext.Current.CancellationToken);
            }
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var suspendHandler = scope.ServiceProvider.GetRequiredService<SuspendApplicationFeature.Handler>();
            var result = await suspendHandler.HandleAsync(applicationId, "Policy violation", TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess);
        }

        var unpublished = await PollForConditionAsync(userId, d => !d.IsPublished);
        Assert.NotNull(unpublished);
        Assert.False(unpublished.IsPublished);

        using (var scope = fixture.Services.CreateScope())
        {
            var availabilityDb = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
            TutorBookability? bookability = null;
            for (var attempt = 0; attempt < 50; attempt++)
            {
                bookability = await availabilityDb.TutorBookabilityRecords.AsNoTracking().FirstOrDefaultAsync(
                    b => b.TutorUserId == userId, TestContext.Current.CancellationToken);
                if (bookability is { IsBookable: false })
                {
                    break;
                }

                await Task.Delay(100, TestContext.Current.CancellationToken);
            }

            Assert.NotNull(bookability);
            Assert.False(bookability.IsBookable);
        }
    }

    [Fact]
    public async Task TutorPublishedConsumer_RedeliveredAfterCrashBetweenProjectorCommitAndSignalPublish_RecoversTheSignal()
    {
        // A44 replay-safety fix. Simulates the narrow window identified by decompiling Wolverine
        // 6.45.0 under Lightweight transaction mode: TutorBookabilityProjector's SaveChangesAsync
        // commits independently of AvailabilitySignalPublisher's later publish, so a process
        // crash between them can leave bookability flipped with the signal never sent. Since
        // HandleAsync never returned, the incoming TutorPublished envelope was never marked
        // handled, so Wolverine redelivers it — reproduced here directly rather than literally
        // crashing a process. Proves the redelivered call still recovers the signal even though
        // the bookability row was already correct from the prior attempt (never conditioned on
        // the projector actually changing a row).
        var userId = Guid.NewGuid();
        Guid applicationId;

        using (var scope = fixture.Services.CreateScope())
        {
            var tutorsDb = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var clock = TimeProvider.System;
            var application = TutorApplication.CreateDraft(userId, clock);
            application.ApplyDraftUpdate(
                new DraftUpdate(
                    DisplayName: "Crash Recovery Tutor",
                    Headline: "Recovers after a crash",
                    ProfilePhotoReference: "https://example.com/photo.jpg",
                    IntroVideoReference: "https://example.com/intro.mp4",
                    SubjectIds: [Guid.NewGuid()],
                    SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                    HourlyRate: new Money(2200, "EUR")),
                clock);
            tutorsDb.TutorApplications.Add(application);
            application.Submit(clock);
            application.BeginReview(clock);
            application.Approve(clock);
            await tutorsDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            applicationId = application.Id;
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<PublishApplicationFeature.Handler>();
            await handler.HandleAsync(applicationId, TestContext.Current.CancellationToken);
        }

        await PollForSearchDocumentAsync(userId);

        DateTimeOffset publishedAtUtc;
        using (var scope = fixture.Services.CreateScope())
        {
            var availabilityDb = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
            var bookability = await availabilityDb.TutorBookabilityRecords.AsNoTracking().FirstAsync(
                b => b.TutorUserId == userId, TestContext.Current.CancellationToken);
            publishedAtUtc = bookability.LastEventAtUtc;

            // Weekly availability configured directly (not via the self-service handler, which
            // would itself publish a fresh signal and mask what this test needs to prove) —
            // gives the live recompute below something real to find.
            var clock = TimeProvider.System;
            var settings = BookingSettings.Create(userId, "UTC", minNoticeHours: 0, maxBookingWindowDays: 7, defaultSlotDurationMinutes: 30, clock);
            Assert.True(settings.IsSuccess);
            availabilityDb.BookingSettings.Add(settings.Value);
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                var rule = WeeklyAvailabilityRule.Create(userId, day, new TimeOnly(0, 0), new TimeOnly(23, 30));
                Assert.True(rule.IsSuccess);
                availabilityDb.WeeklyAvailabilityRules.Add(rule.Value);
            }

            await availabilityDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Redeliver the ORIGINAL TutorPublished event directly against the Availability
        // consumer — same OccurredAtUtc as what's already recorded, exactly matching an
        // at-least-once retry of the same envelope. TryApply will find it stale/equal and skip
        // the row write, but the signal must still be recomputed live and published.
        using (var scope = fixture.Services.CreateScope())
        {
            var projector = scope.ServiceProvider.GetRequiredService<TutorBookabilityProjector>();
            var signalPublisher = scope.ServiceProvider.GetRequiredService<AvailabilitySignalPublisher>();
            var consumer = new AvailabilityTutorPublishedConsumer(projector, signalPublisher);
            await consumer.HandleAsync(BuildTutorPublished(userId, publishedAtUtc), TestContext.Current.CancellationToken);
        }

        var document = await PollForConditionAsync(userId, d => d.HasUpcomingAvailability);
        Assert.NotNull(document);
        Assert.True(document.HasUpcomingAvailability);
    }

    [Fact]
    public async Task TutorPublishedConsumer_StaleRedeliveryAfterALaterSuspend_DoesNotReinstateBookability()
    {
        // A44 replay-safety fix: a delayed/out-of-order redelivery of the ORIGINAL TutorPublished
        // event, arriving after a later TutorUnpublished has already superseded it, must not flip
        // bookability back to true.
        var userId = Guid.NewGuid();
        Guid applicationId;

        using (var scope = fixture.Services.CreateScope())
        {
            var tutorsDb = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
            var clock = TimeProvider.System;
            var application = TutorApplication.CreateDraft(userId, clock);
            application.ApplyDraftUpdate(
                new DraftUpdate(
                    DisplayName: "Stale Event Tutor",
                    Headline: "Suspended before a stale retry arrives",
                    ProfilePhotoReference: "https://example.com/photo.jpg",
                    IntroVideoReference: "https://example.com/intro.mp4",
                    SubjectIds: [Guid.NewGuid()],
                    SpokenLanguages: [new TutorSpokenLanguage("en", LanguageProficiency.Native)],
                    HourlyRate: new Money(2200, "EUR")),
                clock);
            tutorsDb.TutorApplications.Add(application);
            application.Submit(clock);
            application.BeginReview(clock);
            application.Approve(clock);
            await tutorsDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            applicationId = application.Id;
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<PublishApplicationFeature.Handler>();
            await handler.HandleAsync(applicationId, TestContext.Current.CancellationToken);
        }

        await PollForSearchDocumentAsync(userId);

        DateTimeOffset publishedAtUtc;
        using (var scope = fixture.Services.CreateScope())
        {
            var availabilityDb = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
            var bookability = await availabilityDb.TutorBookabilityRecords.AsNoTracking().FirstAsync(
                b => b.TutorUserId == userId, TestContext.Current.CancellationToken);
            publishedAtUtc = bookability.LastEventAtUtc;
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var suspendHandler = scope.ServiceProvider.GetRequiredService<SuspendApplicationFeature.Handler>();
            var result = await suspendHandler.HandleAsync(applicationId, "Policy violation", TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess);
        }

        // Wait for the real suspend-driven TutorUnpublished to land before delivering the stale
        // retry, so there's a genuine "later" state for the stale message to (wrongly) try to
        // undo.
        TutorBookability? suspended = null;
        using (var scope = fixture.Services.CreateScope())
        {
            var availabilityDb = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
            for (var attempt = 0; attempt < 50; attempt++)
            {
                suspended = await availabilityDb.TutorBookabilityRecords.AsNoTracking().FirstOrDefaultAsync(
                    b => b.TutorUserId == userId, TestContext.Current.CancellationToken);
                if (suspended is { IsBookable: false })
                {
                    break;
                }

                await Task.Delay(100, TestContext.Current.CancellationToken);
            }
        }

        Assert.NotNull(suspended);
        Assert.False(suspended!.IsBookable);

        using (var scope = fixture.Services.CreateScope())
        {
            var projector = scope.ServiceProvider.GetRequiredService<TutorBookabilityProjector>();
            var signalPublisher = scope.ServiceProvider.GetRequiredService<AvailabilitySignalPublisher>();
            var consumer = new AvailabilityTutorPublishedConsumer(projector, signalPublisher);
            await consumer.HandleAsync(BuildTutorPublished(userId, publishedAtUtc), TestContext.Current.CancellationToken);
        }

        using var finalScope = fixture.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        var final = await finalDb.TutorBookabilityRecords.AsNoTracking().FirstAsync(
            b => b.TutorUserId == userId, TestContext.Current.CancellationToken);
        Assert.False(final.IsBookable);
    }

    private static TutorPublished BuildTutorPublished(Guid tutorUserId, DateTimeOffset occurredAtUtc) => new(
        TutorUserId: tutorUserId,
        Slug: "replay-test-tutor",
        DisplayName: "Replay Test Tutor",
        Headline: "Replay test",
        PhotoUrl: null,
        SubjectIds: [],
        SpecialtyIds: [],
        SpokenLanguages: [],
        CountryOfBirthCode: null,
        IsProfessionalTutor: false,
        IsSuperTutor: false,
        HourlyRateAmountMinorUnits: 2200,
        HourlyRateCurrency: "EUR",
        PublishedAtUtc: occurredAtUtc,
        OccurredAtUtc: occurredAtUtc);

    // Wolverine's local in-process transport delivers asynchronously — PublishAsync enqueues
    // and returns, it does not wait for the consumer to finish (correct production semantics:
    // the publisher is never blocked on a subscriber). These helpers are the test-side
    // equivalent of Wolverine's own tracked-session test helpers, without pulling in that extra
    // package for a handful of assertions.
    private async Task<TutorSearchDocument?> PollForSearchDocumentAsync(Guid tutorUserId) =>
        await PollForConditionAsync(tutorUserId, _ => true);

    private async Task<TutorSearchDocument?> PollForConditionAsync(Guid tutorUserId, Func<TutorSearchDocument, bool> condition)
    {
        using var scope = fixture.Services.CreateScope();
        var searchDb = scope.ServiceProvider.GetRequiredService<SearchDbContext>();

        TutorSearchDocument? document = null;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            document = await searchDb.TutorSearchDocuments.AsNoTracking().FirstOrDefaultAsync(
                d => d.TutorUserId == tutorUserId, TestContext.Current.CancellationToken);
            if (document is not null && condition(document))
            {
                break;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        return document;
    }
}
