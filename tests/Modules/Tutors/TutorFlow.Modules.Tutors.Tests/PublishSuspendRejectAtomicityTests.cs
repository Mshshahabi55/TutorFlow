using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using Wolverine;
using Wolverine.Tracking;
using GetCurrentApplicationFeature = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication;
using PublishApplicationFeature = TutorFlow.Modules.Tutors.Features.PublishApplication.PublishApplication;
using RejectCertificateFeature = TutorFlow.Modules.Tutors.Features.RejectCertificate.RejectCertificate;
using SuspendApplicationFeature = TutorFlow.Modules.Tutors.Features.SuspendApplication.SuspendApplication;

namespace TutorFlow.Modules.Tutors.Tests;

/// <summary>
/// A44 #105: proves the specific property #103's fix exists to guarantee for the three
/// Lightweight-mode/idempotent-recovery publish paths — a business-transaction failure does not
/// leave a misleading outgoing event behind, and a retried already-applied action republishes
/// rather than silently losing the event. <c>TutorPublishedPipelineTests</c> (Host.Tests) and
/// <c>CertificateRejectionNotificationTests</c> already prove the success/durability half of
/// this (the event is actually, durably delivered end-to-end); this file covers the half neither
/// of them does — the rollback/no-event-on-failure half.
/// </summary>
[Collection(TutorsHostCollection.Name)]
public sealed class PublishSuspendRejectAtomicityTests(TutorsApiFixture fixture) : IClassFixture<TutorsApiFixture>
{
    private async Task<Guid> SeedDraftApplicationAsync(Guid userId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var application = TutorApplication.CreateDraft(userId, TimeProvider.System);
        db.TutorApplications.Add(application);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return application.Id;
    }

    private async Task<Guid> SeedApprovedApplicationAsync(Guid userId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var clock = TimeProvider.System;
        var application = TutorApplication.CreateDraft(userId, clock);
        application.ApplyDraftUpdate(
            new DraftUpdate(DisplayName: "Atomicity Test Tutor", HourlyRate: new Money(2000, "EUR")),
            clock);
        db.TutorApplications.Add(application);
        application.Submit(clock);
        application.BeginReview(clock);
        application.Approve(clock);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return application.Id;
    }

    [Fact]
    public async Task PublishApplication_WhenApplicationIsStillDraft_FailsWithoutPublishingTutorPublished()
    {
        var applicationId = await SeedDraftApplicationAsync(Guid.NewGuid());

        TutorFlow.Modules.Tutors.Domain.Result<GetCurrentApplicationFeature.Response>? result = null;
        var session = await fixture.Services.ExecuteAndWaitAsync(async () =>
        {
            using var scope = fixture.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            result = await bus.InvokeAsync<TutorFlow.Modules.Tutors.Domain.Result<GetCurrentApplicationFeature.Response>>(
                new PublishApplicationFeature.Command(applicationId), TestContext.Current.CancellationToken);
        }, timeoutInMilliseconds: 10_000);

        Assert.NotNull(result);
        Assert.True(result.IsFailure);

        // The decisive assertion: a domain-validation failure before the handler ever calls
        // bus.PublishAsync must mean no TutorPublished envelope was ever enqueued, let alone
        // persisted — not merely that the test happened not to observe one.
        Assert.Empty(session.Sent.MessagesOf<TutorPublished>());

        using var verifyScope = fixture.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var application = await db.TutorApplications.AsNoTracking().FirstAsync(
            a => a.Id == applicationId, TestContext.Current.CancellationToken);
        Assert.Equal(TutorApplicationStatus.Draft, application.Status);
    }

    [Fact]
    public async Task SuspendApplication_WhenApplicationIsStillDraft_FailsWithoutPublishingTutorUnpublished()
    {
        var applicationId = await SeedDraftApplicationAsync(Guid.NewGuid());

        TutorFlow.Modules.Tutors.Domain.Result? result = null;
        var session = await fixture.Services.ExecuteAndWaitAsync(async () =>
        {
            using var scope = fixture.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            result = await bus.InvokeAsync<TutorFlow.Modules.Tutors.Domain.Result>(
                new SuspendApplicationFeature.Command(applicationId, "Policy violation", Guid.NewGuid()), TestContext.Current.CancellationToken);
        }, timeoutInMilliseconds: 10_000);

        Assert.NotNull(result);
        Assert.True(result.IsFailure);
        Assert.Empty(session.Sent.MessagesOf<TutorUnpublished>());

        using var verifyScope = fixture.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var application = await db.TutorApplications.AsNoTracking().FirstAsync(
            a => a.Id == applicationId, TestContext.Current.CancellationToken);
        Assert.Equal(TutorApplicationStatus.Draft, application.Status);
    }

    [Fact]
    public async Task RejectCertificate_WhenCertificateDoesNotExist_FailsWithoutPublishingTutorCertificateRejected()
    {
        var applicationId = await SeedApprovedApplicationAsync(Guid.NewGuid());
        var nonExistentCertificateId = Guid.NewGuid();

        TutorFlow.Modules.Tutors.Domain.Result? result = null;
        var session = await fixture.Services.ExecuteAndWaitAsync(async () =>
        {
            using var scope = fixture.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            result = await bus.InvokeAsync<TutorFlow.Modules.Tutors.Domain.Result>(
                new RejectCertificateFeature.Dispatch(applicationId, nonExistentCertificateId, "Illegible.", Guid.NewGuid()),
                TestContext.Current.CancellationToken);
        }, timeoutInMilliseconds: 10_000);

        Assert.NotNull(result);
        Assert.True(result.IsFailure);
        Assert.Empty(session.Sent.MessagesOf<TutorCertificateRejected>());
    }

    [Fact]
    public async Task RejectCertificate_RetriedAfterAlreadyRejected_RepublishesInsteadOfFailing()
    {
        var userId = Guid.NewGuid();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var clock = TimeProvider.System;
        var application = TutorApplication.CreateDraft(userId, clock);
        var draftUpdate = application.ApplyDraftUpdate(
            new DraftUpdate(
                DisplayName: "Idempotent Reject Tutor",
                HourlyRate: new Money(2000, "EUR"),
                CertificateEntries: [new CertificateEntryDraftInput(null, "TEFL Certificate", "ITA", null, null, null, null)]),
            clock);
        Assert.True(draftUpdate.IsSuccess);
        db.TutorApplications.Add(application);
        var certificateId = application.CertificateEntries.Single().Id;
        // RejectCertificate requires PendingReview — only reachable by actually submitting a
        // document, not merely by the certificate entry existing (its default status is None).
        var submitResult = application.SubmitCertificateDocument(certificateId, "certificates/test.pdf", clock);
        Assert.True(submitResult.IsSuccess);
        application.Submit(clock);
        application.BeginReview(clock);
        application.Approve(clock);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // First rejection — the real, non-idempotent path.
        var first = await bus.InvokeAsync<TutorFlow.Modules.Tutors.Domain.Result>(
            new RejectCertificateFeature.Dispatch(application.Id, certificateId, "Illegible.", Guid.NewGuid()),
            TestContext.Current.CancellationToken);
        Assert.True(first.IsSuccess);

        // Retried as if the caller never saw the first response (e.g. a dropped connection) —
        // must republish TutorCertificateRejected from persisted state, not fail with "already
        // rejected", and must not attempt to reject/delete the document a second time.
        TutorFlow.Modules.Tutors.Domain.Result? second = null;
        var session = await fixture.Services.ExecuteAndWaitAsync(async () =>
        {
            using var retryScope = fixture.Services.CreateScope();
            var retryBus = retryScope.ServiceProvider.GetRequiredService<IMessageBus>();
            second = await retryBus.InvokeAsync<TutorFlow.Modules.Tutors.Domain.Result>(
                new RejectCertificateFeature.Dispatch(application.Id, certificateId, "Illegible.", Guid.NewGuid()),
                TestContext.Current.CancellationToken);
        }, timeoutInMilliseconds: 10_000);

        Assert.NotNull(second);
        Assert.True(second.IsSuccess);
        Assert.NotEmpty(session.Sent.MessagesOf<TutorCertificateRejected>());
    }
}
