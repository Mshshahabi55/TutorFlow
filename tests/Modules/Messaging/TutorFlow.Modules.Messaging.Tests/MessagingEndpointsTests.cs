using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Messaging.Infrastructure;
using TutorFlow.Modules.Messaging.Persistence;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Messaging.Tests;

/// <summary>
/// ADR-0030: Messaging V1's core correctness — conversation creation/eligibility, participant
/// authorization, sender-identity integrity, and read-state ownership. Seeds real
/// <c>TutorBookability</c> rows (not mocks) so the eligibility check exercises genuine
/// cross-module DI, matching this codebase's established convention.
/// </summary>
[Collection(MessagingHostCollection.Name)]
public class MessagingEndpointsTests(MessagingApiFixture fixture) : IClassFixture<MessagingApiFixture>
{
    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", MessagingApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    private async Task SeedTutorBookabilityAsync(Guid tutorUserId, bool isBookable)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        db.TutorBookabilityRecords.Add(TutorBookability.Create(tutorUserId, isBookable, TimeProvider.System));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    // Seeds a real, fully Published TutorApplication (not just a bare TutorBookabilityRecords
    // row) so ITutorProfileSummaryQuery has genuine DisplayName/ProfilePhotoReference/PublicSlug
    // data to resolve (ADR-0030 Batch 69) — exercises the real cross-module query, not a mock.
    private async Task<(Guid TutorUserId, string DisplayName, string ProfilePhotoReference, string PublicSlug)> SeedPublishedTutorAsync(string displayName)
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
        Assert.True(application.Submit(clock).IsSuccess);
        Assert.True(application.BeginReview(clock).IsSuccess);
        Assert.True(application.Approve(clock).IsSuccess);
        Assert.True(application.Publish(clock).IsSuccess);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (application.UserId, displayName, application.ProfilePhotoReference!, application.PublicSlug!);
    }

    // Flips an existing Tutor's current bookability to false directly in Availability's own
    // projection (the same effect TutorUnpublishedConsumer has on a real suspend) — used to prove
    // an existing conversation remains readable, and the Tutor's display info still resolves via
    // ITutorProfileSummaryQuery (which never reads TutorBookabilityRecords at all), after the
    // Tutor is no longer currently bookable (ADR-0030's "history persists past suspension" intent).
    private async Task SetTutorNoLongerBookableAsync(Guid tutorUserId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        var affected = await db.TutorBookabilityRecords
            .Where(b => b.TutorUserId == tutorUserId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsBookable, false), TestContext.Current.CancellationToken);
        Assert.Equal(1, affected);
    }

    private async Task<Guid> CreateConversationAsync(Guid studentUserId, Guid tutorUserId)
    {
        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        var response = await client.PostAsJsonAsync(
            "/api/messaging/conversations", new { tutorUserId }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ConversationResponse>(TestContext.Current.CancellationToken);
        return body!.Id;
    }

    private sealed record TutorDisplaySummaryDto(string DisplayName, string? ProfilePhotoReference, string? PublicSlug);
    private sealed record ConversationResponse(Guid Id, Guid StudentUserId, Guid TutorUserId, DateTimeOffset CreatedAtUtc);
    private sealed record ConversationSummaryDto(
        Guid Id, Guid StudentUserId, Guid TutorUserId, DateTimeOffset CreatedAtUtc,
        DateTimeOffset? LastMessageAtUtc, DateTimeOffset? MyLastReadAtUtc, TutorDisplaySummaryDto? Tutor);
    private sealed record MessageResponse(Guid Id, Guid SenderUserId, string Body, DateTimeOffset CreatedAtUtc, long SequenceNumber);
    private sealed record MessagesPageResponse(TutorDisplaySummaryDto? Tutor, List<MessageResponse> Messages, long? NextBeforeCursor);

    [Fact]
    public async Task CreateConversation_PublishedTutor_NoBookingRequired_Succeeds()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        var response = await client.PostAsJsonAsync(
            "/api/messaging/conversations", new { tutorUserId }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConversationResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(studentUserId, body!.StudentUserId);
        Assert.Equal(tutorUserId, body.TutorUserId);
    }

    [Fact]
    public async Task CreateConversation_UnpublishedTutor_AndNonexistentTutor_ProduceTheSameRejection()
    {
        var unpublishedTutorId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(unpublishedTutorId, isBookable: false);
        var nonexistentTutorId = Guid.NewGuid(); // never seeded at all — no TutorBookability row exists

        using var client = CreateAuthenticatedClient(Guid.NewGuid(), RoleNames.Student);

        var unpublishedResponse = await client.PostAsJsonAsync(
            "/api/messaging/conversations", new { tutorUserId = unpublishedTutorId }, TestContext.Current.CancellationToken);
        var nonexistentResponse = await client.PostAsJsonAsync(
            "/api/messaging/conversations", new { tutorUserId = nonexistentTutorId }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, unpublishedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, nonexistentResponse.StatusCode);

        var unpublishedBody = await unpublishedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var nonexistentBody = await nonexistentResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(unpublishedBody, nonexistentBody);
    }

    [Fact]
    public async Task CreateConversation_TutorNeverPublished_NoBookabilityRecordAtAll_FailsClosed()
    {
        // Fail-closed (ADR-0014/ADR-0030): absence of a TutorBookability row — the real-world
        // state for a Draft/Submitted/UnderReview tutor that has never been published — must
        // never default to eligible.
        var draftTutorId = Guid.NewGuid();

        using var client = CreateAuthenticatedClient(Guid.NewGuid(), RoleNames.Student);
        var response = await client.PostAsJsonAsync(
            "/api/messaging/conversations", new { tutorUserId = draftTutorId }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateConversation_CalledTwiceForTheSamePair_ReturnsTheSameConversation_NotADuplicate()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        var first = await client.PostAsJsonAsync("/api/messaging/conversations", new { tutorUserId }, TestContext.Current.CancellationToken);
        var second = await client.PostAsJsonAsync("/api/messaging/conversations", new { tutorUserId }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstBody = await first.Content.ReadFromJsonAsync<ConversationResponse>(TestContext.Current.CancellationToken);
        var secondBody = await second.Content.ReadFromJsonAsync<ConversationResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(firstBody!.Id, secondBody!.Id);
    }

    [Fact]
    public async Task TutorCannotInitiateANewConversation()
    {
        var tutorUserId = Guid.NewGuid();
        var otherTutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(otherTutorUserId, isBookable: true);

        using var client = CreateAuthenticatedClient(tutorUserId, RoleNames.Tutor);
        var response = await client.PostAsJsonAsync(
            "/api/messaging/conversations", new { tutorUserId = otherTutorUserId }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TutorCanReplyToAnExistingConversation()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        var conversationId = await CreateConversationAsync(studentUserId, tutorUserId);

        using var tutorClient = CreateAuthenticatedClient(tutorUserId, RoleNames.Tutor);
        var response = await tutorClient.PostAsJsonAsync(
            $"/api/messaging/conversations/{conversationId}/messages", new { text = "Happy to help!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(tutorUserId, body!.SenderUserId);
    }

    [Fact]
    public async Task SenderIdentity_IsAlwaysDerivedFromTheAuthenticatedPrincipal_NeverFromTheRequestBody()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        var conversationId = await CreateConversationAsync(studentUserId, tutorUserId);

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        // The command DTO has no sender field at all, but even if a client appends an unknown
        // "senderUserId" field by hand, it has nothing to bind to and cannot influence the
        // persisted SenderUserId.
        var payload = new Dictionary<string, object> { ["text"] = "Hi!", ["senderUserId"] = tutorUserId };
        var response = await client.PostAsJsonAsync(
            $"/api/messaging/conversations/{conversationId}/messages", payload, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(studentUserId, body!.SenderUserId);
    }

    [Fact]
    public async Task NonParticipant_CannotReadMessages_CannotSendMessages_CannotMarkRead()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        var conversationId = await CreateConversationAsync(studentUserId, tutorUserId);

        var strangerUserId = Guid.NewGuid();
        using var strangerClient = CreateAuthenticatedClient(strangerUserId, RoleNames.Student);

        var readResponse = await strangerClient.GetAsync(
            $"/api/messaging/conversations/{conversationId}/messages", TestContext.Current.CancellationToken);
        var sendResponse = await strangerClient.PostAsJsonAsync(
            $"/api/messaging/conversations/{conversationId}/messages", new { text = "intrusion" }, TestContext.Current.CancellationToken);
        var markReadResponse = await strangerClient.PostAsync(
            $"/api/messaging/conversations/{conversationId}/read", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, sendResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, markReadResponse.StatusCode);
    }

    [Fact]
    public async Task NonexistentConversation_AndConversationBelongingToSomeoneElse_ProduceTheSameNotFound()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        var realConversationId = await CreateConversationAsync(studentUserId, tutorUserId);

        var strangerUserId = Guid.NewGuid();
        using var strangerClient = CreateAuthenticatedClient(strangerUserId, RoleNames.Student);

        var nonexistentResponse = await strangerClient.GetAsync(
            $"/api/messaging/conversations/{Guid.NewGuid()}/messages", TestContext.Current.CancellationToken);
        var notMineResponse = await strangerClient.GetAsync(
            $"/api/messaging/conversations/{realConversationId}/messages", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, nonexistentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notMineResponse.StatusCode);
    }

    [Fact]
    public async Task ListConversations_NeverReturnsAnotherUsersConversations()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        await CreateConversationAsync(studentUserId, tutorUserId);

        var unrelatedUserId = Guid.NewGuid();
        using var unrelatedClient = CreateAuthenticatedClient(unrelatedUserId, RoleNames.Student);
        var response = await unrelatedClient.GetFromJsonAsync<List<ConversationResponse>>(
            "/api/messaging/conversations", TestContext.Current.CancellationToken);

        Assert.Empty(response!);
    }

    [Fact]
    public async Task EachParticipant_CanOnlyUpdateTheirOwnReadState()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        var conversationId = await CreateConversationAsync(studentUserId, tutorUserId);

        using var studentClient = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        var markReadResponse = await studentClient.PostAsync(
            $"/api/messaging/conversations/{conversationId}/read", content: null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, markReadResponse.StatusCode);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
        var conversation = await db.Conversations.AsNoTracking()
            .FirstAsync(c => c.Id == conversationId, TestContext.Current.CancellationToken);

        Assert.NotNull(conversation.StudentLastReadAtUtc);
        Assert.Null(conversation.TutorLastReadAtUtc);
    }

    [Fact]
    public async Task SendMessage_WhitespaceOnlyBody_IsRejected()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        var conversationId = await CreateConversationAsync(studentUserId, tutorUserId);

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        var response = await client.PostAsJsonAsync(
            $"/api/messaging/conversations/{conversationId}/messages", new { text = "    " }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_OverLengthBody_IsRejected()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        var conversationId = await CreateConversationAsync(studentUserId, tutorUserId);

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        var response = await client.PostAsJsonAsync(
            $"/api/messaging/conversations/{conversationId}/messages",
            new { text = new string('a', 5001) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_CursorPagination_IsDeterministic_NewestFirst_AndPagesIntoOlderHistory()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        var conversationId = await CreateConversationAsync(studentUserId, tutorUserId);

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync(
                $"/api/messaging/conversations/{conversationId}/messages", new { text = $"message {i}" }, TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
        }

        var firstPage = await client.GetFromJsonAsync<MessagesPageResponse>(
            $"/api/messaging/conversations/{conversationId}/messages?take=2", TestContext.Current.CancellationToken);
        Assert.Equal(2, firstPage!.Messages.Count);
        Assert.Equal("message 4", firstPage.Messages[0].Body);
        Assert.Equal("message 3", firstPage.Messages[1].Body);
        Assert.NotNull(firstPage.NextBeforeCursor);

        var secondPage = await client.GetFromJsonAsync<MessagesPageResponse>(
            $"/api/messaging/conversations/{conversationId}/messages?take=2&before={firstPage.NextBeforeCursor}", TestContext.Current.CancellationToken);
        Assert.Equal(2, secondPage!.Messages.Count);
        Assert.Equal("message 2", secondPage.Messages[0].Body);
        Assert.Equal("message 1", secondPage.Messages[1].Body);

        // No two pages share a message, and together they account for exactly the messages
        // requested so far — proves the cursor neither skips nor duplicates across page calls.
        var seenIds = firstPage.Messages.Concat(secondPage.Messages).Select(m => m.Id).ToList();
        Assert.Equal(seenIds.Count, seenIds.Distinct().Count());
    }

    // --- Batch 69: Tutor display-identity enrichment (ADR-0030) ---

    [Fact]
    public async Task ListConversations_IncludesCorrectTutorDisplaySummary()
    {
        var studentUserId = Guid.NewGuid();
        var (tutorUserId, displayName, profilePhoto, slug) = await SeedPublishedTutorAsync("Ada Lovelace");
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        await CreateConversationAsync(studentUserId, tutorUserId);

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        var conversations = await client.GetFromJsonAsync<List<ConversationSummaryDto>>(
            "/api/messaging/conversations", TestContext.Current.CancellationToken);

        var conversation = Assert.Single(conversations!, c => c.TutorUserId == tutorUserId);
        Assert.NotNull(conversation.Tutor);
        Assert.Equal(displayName, conversation.Tutor!.DisplayName);
        Assert.Equal(profilePhoto, conversation.Tutor.ProfilePhotoReference);
        Assert.Equal(slug, conversation.Tutor.PublicSlug);
    }

    [Fact]
    public async Task GetMessages_IncludesCorrectTutorDisplaySummary_OncePerResponse_NotPerMessage()
    {
        var studentUserId = Guid.NewGuid();
        var (tutorUserId, displayName, profilePhoto, slug) = await SeedPublishedTutorAsync("Grace Hopper");
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        var conversationId = await CreateConversationAsync(studentUserId, tutorUserId);

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        await client.PostAsJsonAsync($"/api/messaging/conversations/{conversationId}/messages", new { text = "hi" }, TestContext.Current.CancellationToken);

        var page = await client.GetFromJsonAsync<MessagesPageResponse>(
            $"/api/messaging/conversations/{conversationId}/messages", TestContext.Current.CancellationToken);

        Assert.NotNull(page!.Tutor);
        Assert.Equal(displayName, page.Tutor!.DisplayName);
        Assert.Equal(profilePhoto, page.Tutor.ProfilePhotoReference);
        Assert.Equal(slug, page.Tutor.PublicSlug);

        // The raw JSON itself carries Tutor at the response root exactly once — never duplicated
        // onto each message in the array.
        var raw = await client.GetStringAsync(new Uri($"/api/messaging/conversations/{conversationId}/messages", UriKind.Relative), TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(raw);
        Assert.True(json.RootElement.TryGetProperty("tutor", out _));
        foreach (var message in json.RootElement.GetProperty("messages").EnumerateArray())
        {
            Assert.False(message.TryGetProperty("tutor", out _));
        }
    }

    [Fact]
    public async Task TutorDisplaySummary_SerializesOnlyTheApprovedSafeFields()
    {
        var studentUserId = Guid.NewGuid();
        var (tutorUserId, _, _, _) = await SeedPublishedTutorAsync("Katherine Johnson");
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        await CreateConversationAsync(studentUserId, tutorUserId);

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        var raw = await client.GetStringAsync(new Uri("/api/messaging/conversations", UriKind.Relative), TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(raw);

        var tutor = json.RootElement.EnumerateArray().Single(c => c.GetProperty("tutorUserId").GetGuid() == tutorUserId).GetProperty("tutor");

        var propertyNames = tutor.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(["displayName", "profilePhotoReference", "publicSlug"], propertyNames.OrderBy(n => n, StringComparer.Ordinal));

        // Negative assertions: nothing resembling email, internal application status, or any
        // other private TutorApplication field is ever present (ADR-0030 Batch 69 privacy review).
        Assert.False(tutor.TryGetProperty("email", out _));
        Assert.False(tutor.TryGetProperty("status", out _));
        Assert.False(tutor.TryGetProperty("rejectionReason", out _));
        Assert.False(tutor.TryGetProperty("suspensionReason", out _));
    }

    [Fact]
    public async Task ConversationSummary_StudentSide_NeverCarriesAnyStudentIdentityFieldBeyondTheRawUserId()
    {
        var studentUserId = Guid.NewGuid();
        var (tutorUserId, _, _, _) = await SeedPublishedTutorAsync("Margaret Hamilton");
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        await CreateConversationAsync(studentUserId, tutorUserId);

        using var tutorClient = CreateAuthenticatedClient(tutorUserId, RoleNames.Tutor);
        var raw = await tutorClient.GetStringAsync(new Uri("/api/messaging/conversations", UriKind.Relative), TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(raw);

        var conversation = json.RootElement.EnumerateArray().Single(c => c.GetProperty("studentUserId").GetGuid() == studentUserId);
        var propertyNames = conversation.EnumerateObject().Select(p => p.Name).ToList();

        // Exactly the established V1 fields plus the new Tutor field — no "student" display field
        // was added anywhere (approved product decision: generic "Student" label, frontend-only).
        Assert.Equal(
            new[] { "createdAtUtc", "id", "lastMessageAtUtc", "myLastReadAtUtc", "studentUserId", "tutor", "tutorUserId" },
            propertyNames.OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetMessages_HistoricalConversation_RemainsReadable_AndTutorSummaryStillResolves_WhenTutorIsNoLongerCurrentlyBookable()
    {
        var studentUserId = Guid.NewGuid();
        var (tutorUserId, displayName, _, _) = await SeedPublishedTutorAsync("Hedy Lamarr");
        await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
        var conversationId = await CreateConversationAsync(studentUserId, tutorUserId);

        // The Tutor is no longer currently bookable — a *new* conversation could no longer be
        // created with them, but this *existing* one, and the Tutor's last-known display info,
        // must remain fully readable for its genuine participants.
        await SetTutorNoLongerBookableAsync(tutorUserId);

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        var response = await client.GetAsync(
            new Uri($"/api/messaging/conversations/{conversationId}/messages", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<MessagesPageResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(page!.Tutor);
        Assert.Equal(displayName, page.Tutor!.DisplayName);
    }

    [Fact]
    public async Task ListConversations_ReturnsAtMostFiftyEntries_PreservingMostRecentActivityOrdering()
    {
        var studentUserId = Guid.NewGuid();
        var expectedMostRecentTutorIds = new List<Guid>();

        // One over the cap — the oldest-activity conversation must be the one excluded.
        for (var i = 0; i < ListConversationsFeatureMaxConversations + 1; i++)
        {
            var (tutorUserId, _, _, _) = await SeedPublishedTutorAsync($"Tutor {i}");
            await SeedTutorBookabilityAsync(tutorUserId, isBookable: true);
            var conversationId = await CreateConversationAsync(studentUserId, tutorUserId);

            // Stamp a strictly increasing LastMessageAtUtc per iteration so ordering is
            // deterministic rather than relying on real-clock timing between fast iterations.
            using var scope = fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
            await db.Conversations.Where(c => c.Id == conversationId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.LastMessageAtUtc, DateTimeOffset.UtcNow.AddSeconds(i)), TestContext.Current.CancellationToken);

            if (i > 0) // the first (oldest) iteration is expected to be the one dropped by the cap
            {
                expectedMostRecentTutorIds.Add(tutorUserId);
            }
        }

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        var conversations = await client.GetFromJsonAsync<List<ConversationSummaryDto>>(
            "/api/messaging/conversations", TestContext.Current.CancellationToken);

        Assert.Equal(ListConversationsFeatureMaxConversations, conversations!.Count);
        Assert.Equal(expectedMostRecentTutorIds.Count, conversations.Select(c => c.TutorUserId).Intersect(expectedMostRecentTutorIds).Count());
    }

    private const int ListConversationsFeatureMaxConversations =
        TutorFlow.Modules.Messaging.Features.ListConversations.ListConversations.Handler.MaxConversations;
}
