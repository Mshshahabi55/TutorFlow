using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Messaging.Infrastructure;

namespace TutorFlow.Modules.Messaging.Tests;

/// <summary>
/// ADR-0030: proves the messaging-create-conversation/messaging-send-message rate-limit policies
/// are wired, partition by the authenticated caller (not IP), and return 429 — using a dedicated,
/// tightly-limited fixture (see <see cref="MessagingRateLimitingApiFixture"/>'s own rationale).
/// </summary>
[Collection(MessagingHostCollection.Name)]
public class MessagingRateLimitingTests(MessagingRateLimitingApiFixture fixture) : IClassFixture<MessagingRateLimitingApiFixture>
{
    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", MessagingApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    private async Task SeedTutorBookabilityAsync(Guid tutorUserId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        db.TutorBookabilityRecords.Add(TutorBookability.Create(tutorUserId, isBookable: true, TimeProvider.System));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateConversation_ExceedingLimit_ReturnsTooManyRequests()
    {
        var studentUserId = Guid.NewGuid();
        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);

        for (var i = 0; i < MessagingRateLimitingApiFixture.CreateConversationPermitLimit; i++)
        {
            var tutorUserId = Guid.NewGuid();
            await SeedTutorBookabilityAsync(tutorUserId);
            await client.PostAsJsonAsync("/api/messaging/conversations", new { tutorUserId }, TestContext.Current.CancellationToken);
        }

        var extraTutorId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(extraTutorId);
        var response = await client.PostAsJsonAsync(
            "/api/messaging/conversations", new { tutorUserId = extraTutorId }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task CreateConversation_DifferentAuthenticatedUsers_DoNotShareRateLimitCounters()
    {
        var exhaustedStudentId = Guid.NewGuid();
        using var exhaustedClient = CreateAuthenticatedClient(exhaustedStudentId, RoleNames.Student);
        for (var i = 0; i < MessagingRateLimitingApiFixture.CreateConversationPermitLimit; i++)
        {
            var tutorUserId = Guid.NewGuid();
            await SeedTutorBookabilityAsync(tutorUserId);
            await exhaustedClient.PostAsJsonAsync("/api/messaging/conversations", new { tutorUserId }, TestContext.Current.CancellationToken);
        }

        var otherStudentId = Guid.NewGuid();
        using var otherClient = CreateAuthenticatedClient(otherStudentId, RoleNames.Student);
        var otherTutorId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(otherTutorId);
        var response = await otherClient.PostAsJsonAsync(
            "/api/messaging/conversations", new { tutorUserId = otherTutorId }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ExceedingLimit_ReturnsTooManyRequests()
    {
        var studentUserId = Guid.NewGuid();
        var tutorUserId = Guid.NewGuid();
        await SeedTutorBookabilityAsync(tutorUserId);

        using var client = CreateAuthenticatedClient(studentUserId, RoleNames.Student);
        var createResponse = await client.PostAsJsonAsync(
            "/api/messaging/conversations", new { tutorUserId }, TestContext.Current.CancellationToken);
        var conversation = await createResponse.Content.ReadFromJsonAsync<ConversationIdOnly>(TestContext.Current.CancellationToken);

        for (var i = 0; i < MessagingRateLimitingApiFixture.SendMessagePermitLimit; i++)
        {
            await client.PostAsJsonAsync(
                $"/api/messaging/conversations/{conversation!.Id}/messages", new { text = $"message {i}" }, TestContext.Current.CancellationToken);
        }

        var response = await client.PostAsJsonAsync(
            $"/api/messaging/conversations/{conversation!.Id}/messages", new { text = "one too many" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    private sealed record ConversationIdOnly(Guid Id);
}
