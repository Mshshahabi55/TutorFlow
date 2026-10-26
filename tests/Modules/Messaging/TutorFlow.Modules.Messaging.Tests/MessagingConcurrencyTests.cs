using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Messaging.Persistence;
using TutorFlow.Shared;
using CreateOrGetConversationFeature = TutorFlow.Modules.Messaging.Features.CreateOrGetConversation.CreateOrGetConversation;
using SendMessageFeature = TutorFlow.Modules.Messaging.Features.SendMessage.SendMessage;

namespace TutorFlow.Modules.Messaging.Tests;

/// <summary>
/// ADR-0030: real-Postgres concurrency proof for the two guarantees this slice depends on —
/// create-or-get converges on one row under a genuine insert race, and the DB-generated
/// SequenceNumber never collides or reorders under genuinely concurrent sends. Two independent
/// DI scopes per race, matching this codebase's established <c>ConcurrencyTests.cs</c>
/// (Bookings)/<c>ReconcileOnceAsync_TwoConcurrentAttempts_...</c> (Notifications) pattern — not a
/// claim that every run deterministically hits the exact race window, only that the guarded,
/// set-based write is safe whenever it does.
/// </summary>
[Collection(MessagingHostCollection.Name)]
public class MessagingConcurrencyTests(MessagingApiFixture fixture) : IClassFixture<MessagingApiFixture>
{
    private async Task SeedTutorBookabilityAsync(Guid tutorUserId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        db.TutorBookabilityRecords.Add(TutorBookability.Create(tutorUserId, isBookable: true, TimeProvider.System));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateOrGetConversation_TwoConcurrentRequestsForTheSamePair_ProduceExactlyOneConversation()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId);

        using var scopeA = fixture.Services.CreateScope();
        using var scopeB = fixture.Services.CreateScope();
        var handlerA = scopeA.ServiceProvider.GetRequiredService<CreateOrGetConversationFeature.Handler>();
        var handlerB = scopeB.ServiceProvider.GetRequiredService<CreateOrGetConversationFeature.Handler>();

        var resultsTask = Task.WhenAll(
            handlerA.HandleAsync(studentUserId, tutorUserId, TestContext.Current.CancellationToken),
            handlerB.HandleAsync(studentUserId, tutorUserId, TestContext.Current.CancellationToken));
        var results = await resultsTask;

        Assert.Equal(results[0].Response!.Id, results[1].Response!.Id);

        using var verifyScope = fixture.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<MessagingDbContext>();
        var count = await db.Conversations.CountAsync(
            c => c.StudentUserId == studentUserId && c.TutorUserId == tutorUserId, TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SendMessage_ConcurrentSendsInTheSameConversation_ReceiveDistinctCorrectlyOrderedSequenceNumbers()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId);

        Guid conversationId;
        using (var createScope = fixture.Services.CreateScope())
        {
            var createHandler = createScope.ServiceProvider.GetRequiredService<CreateOrGetConversationFeature.Handler>();
            var (_, created) = await createHandler.HandleAsync(studentUserId, tutorUserId, TestContext.Current.CancellationToken);
            conversationId = created!.Id;
        }

        using var scopeA = fixture.Services.CreateScope();
        using var scopeB = fixture.Services.CreateScope();
        var handlerA = scopeA.ServiceProvider.GetRequiredService<SendMessageFeature.Handler>();
        var handlerB = scopeB.ServiceProvider.GetRequiredService<SendMessageFeature.Handler>();

        var results = await Task.WhenAll(
            handlerA.HandleAsync(studentUserId, conversationId, new SendMessageFeature.Command("from student"), TestContext.Current.CancellationToken),
            handlerB.HandleAsync(tutorUserId, conversationId, new SendMessageFeature.Command("from tutor"), TestContext.Current.CancellationToken));

        var sequenceNumbers = results.Select(r => r.Response!.SequenceNumber).ToList();
        Assert.Equal(2, sequenceNumbers.Distinct().Count());

        using var verifyScope = fixture.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<MessagingDbContext>();
        var persistedOrder = await db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SequenceNumber)
            .Select(m => m.SequenceNumber)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, persistedOrder.Count);
        Assert.True(persistedOrder[0] < persistedOrder[1]);
    }

    [Fact]
    public async Task CreateOrGetConversation_AlreadyCancelledToken_PropagatesCancellation_NeverSilentlySucceeds()
    {
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId);

        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateOrGetConversationFeature.Handler>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(Guid.NewGuid(), tutorUserId, cts.Token));
    }
}
