using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TutorFlow.Web.Tests;

// End-to-end tests against the real HTTP pipeline for the Communication
// bounded context (docs/adr/ADR-022-communication-and-notifications-architecture.md).
// See IdentityEndpointsTests for the scope discipline these follow
// (Presentation orchestration only) — persistence proofs for the mutating
// endpoints here live in PersistenceIntegrityTests, not this file.
public class CommunicationEndpointsTests : IClassFixture<TutorFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly TutorFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CommunicationEndpointsTests(TutorFlowWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private const string TestPassword = "Test-Password-123!";

    private static string UniqueEmail(string prefix = "test") => $"{prefix}-{Guid.NewGuid():N}@example.com";

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private async Task<HttpResponseMessage> PostWithAuthAsync(string url, object? body, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> GetWithAuthAsync(string url, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await _client.SendAsync(request);
    }

    private async Task<(Guid TutorId, string Token)> RegisterAndLoginTutorAsync()
    {
        var email = UniqueEmail("tutor");
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = email, Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var tutorId = registerBody.GetProperty("value").GetProperty("tutorId").GetGuid();

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        var loginBody = await ReadBodyAsync(loginResponse);
        return (tutorId, loginBody.GetProperty("value").GetProperty("token").GetString()!);
    }

    private async Task<(Guid StudentId, string Token)> RegisterAndLoginStudentAsync()
    {
        var email = UniqueEmail("student");
        var registerResponse = await _client.PostAsJsonAsync(
            "/students", new { Email = email, Password = TestPassword, IsMinor = false });
        var registerBody = await ReadBodyAsync(registerResponse);
        var studentId = registerBody.GetProperty("value").GetProperty("studentId").GetGuid();

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        var loginBody = await ReadBodyAsync(loginResponse);
        return (studentId, loginBody.GetProperty("value").GetProperty("token").GetString()!);
    }

    [Fact]
    public async Task StartConversation_from_a_Student_to_a_Tutor_succeeds()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync("/conversations", new { TargetAccountId = tutorId }, studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(tutorId, body.GetProperty("value").GetProperty("otherParticipantId").GetGuid());
    }

    [Fact]
    public async Task StartConversation_requires_authentication()
    {
        var response = await PostWithAuthAsync("/conversations", new { TargetAccountId = Guid.NewGuid() }, bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // A Tutor has no "browse students" capability to pick a target from —
    // ADR-022's explicit restriction: only Student/Parent-Guardian -> Tutor,
    // or Admin/Staff -> anyone, may start a genuinely NEW conversation.
    [Fact]
    public async Task StartConversation_is_forbidden_when_a_Tutor_tries_to_start_a_new_one()
    {
        var (_, tutorToken) = await RegisterAndLoginTutorAsync();
        var (otherTutorId, _) = await RegisterAndLoginTutorAsync();

        var response = await PostWithAuthAsync("/conversations", new { TargetAccountId = otherTutorId }, tutorToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StartConversation_rejects_messaging_self()
    {
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync("/conversations", new { TargetAccountId = studentId }, studentToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal(
            "StartConversationCommand.CannotMessageSelf", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task StartConversation_is_idempotent_for_the_same_pair()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, studentToken) = await RegisterAndLoginStudentAsync();

        var firstResponse = await PostWithAuthAsync("/conversations", new { TargetAccountId = tutorId }, studentToken);
        var firstBody = await ReadBodyAsync(firstResponse);
        var firstConversationId = firstBody.GetProperty("value").GetProperty("conversationId").GetGuid();

        var secondResponse = await PostWithAuthAsync("/conversations", new { TargetAccountId = tutorId }, studentToken);
        var secondBody = await ReadBodyAsync(secondResponse);
        var secondConversationId = secondBody.GetProperty("value").GetProperty("conversationId").GetGuid();

        Assert.Equal(firstConversationId, secondConversationId);
    }

    [Fact]
    public async Task SendMessage_is_reachable_and_returns_the_sent_message()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, studentToken) = await RegisterAndLoginStudentAsync();
        var startResponse = await PostWithAuthAsync("/conversations", new { TargetAccountId = tutorId }, studentToken);
        var conversationId = (await ReadBodyAsync(startResponse)).GetProperty("value").GetProperty("conversationId").GetGuid();

        var response = await PostWithAuthAsync(
            $"/conversations/{conversationId}/messages", new { Body = "Hello, when are you free?" }, studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.Equal("Hello, when are you free?", body.GetProperty("value").GetProperty("body").GetString());
    }

    [Fact]
    public async Task SendMessage_is_forbidden_for_a_caller_who_is_not_a_party_to_the_conversation()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, studentToken) = await RegisterAndLoginStudentAsync();
        var startResponse = await PostWithAuthAsync("/conversations", new { TargetAccountId = tutorId }, studentToken);
        var conversationId = (await ReadBodyAsync(startResponse)).GetProperty("value").GetProperty("conversationId").GetGuid();

        var (_, outsiderToken) = await RegisterAndLoginStudentAsync();
        var response = await PostWithAuthAsync(
            $"/conversations/{conversationId}/messages", new { Body = "I shouldn't be able to send this." }, outsiderToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetConversationMessages_returns_NotFound_for_a_nonexistent_conversation()
    {
        var (_, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/conversations/{Guid.NewGuid()}/messages", studentToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyConversations_lists_a_conversation_the_caller_started()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, studentToken) = await RegisterAndLoginStudentAsync();
        await PostWithAuthAsync("/conversations", new { TargetAccountId = tutorId }, studentToken);

        var response = await GetWithAuthAsync("/conversations/mine", studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var conversations = body.GetProperty("value").EnumerateArray().ToList();
        Assert.Single(conversations);
        Assert.Equal(tutorId, conversations[0].GetProperty("otherParticipantId").GetGuid());
    }

    [Fact]
    public async Task GetMyNotifications_requires_authentication()
    {
        var response = await GetWithAuthAsync("/notifications/mine", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarkNotificationRead_returns_NotFound_for_a_nonexistent_notification()
    {
        var (_, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync($"/notifications/{Guid.NewGuid()}/read", body: null, studentToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_succeeds_with_zero_notifications()
    {
        var (_, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync("/notifications/mark-all-read", body: null, studentToken);

        response.EnsureSuccessStatusCode();
    }
}
