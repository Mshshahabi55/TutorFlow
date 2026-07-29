using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TutorFlow.Web.Tests;

// End-to-end tests against the real HTTP pipeline for the Meetings bounded
// context (docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md).
// The test host's Meeting:DefaultProvider is configured to "Mock"
// (TutorFlowWebApplicationFactory), so "Start Lesson" exercises a real,
// working success path end-to-end without real vendor credentials.
public class MeetingEndpointsTests : IClassFixture<TutorFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly TutorFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MeetingEndpointsTests(TutorFlowWebApplicationFactory factory)
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

    private async Task<Guid> DeclareOnlineAvailabilityAsync(Guid tutorId, string tutorToken)
    {
        var response = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0, // Online
        }, tutorToken);
        var body = await ReadBodyAsync(response);
        return body.GetProperty("value").GetProperty("availabilitySlotId").GetGuid();
    }

    private async Task<Guid> DeclareInPersonAvailabilityAsync(Guid tutorId, string tutorToken)
    {
        var response = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 1, // InPerson
        }, tutorToken);
        var body = await ReadBodyAsync(response);
        return body.GetProperty("value").GetProperty("availabilitySlotId").GetGuid();
    }

    private async Task<Guid> BookSessionAsync(Guid availabilitySlotId, Guid studentId, string studentToken)
    {
        var response = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = availabilitySlotId,
            StudentId = studentId,
            ParentGuardianId = (Guid?)null,
        }, studentToken);
        var body = await ReadBodyAsync(response);
        return body.GetProperty("value").GetProperty("sessionId").GetGuid();
    }

    private async Task<(Guid SessionId, Guid TutorId, string TutorToken, string StudentToken)> BookOnlineSessionAsync()
    {
        var (tutorId, tutorToken) = await RegisterAndLoginTutorAsync();
        var slotId = await DeclareOnlineAvailabilityAsync(tutorId, tutorToken);
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync();

        var sessionId = await BookSessionAsync(slotId, studentId, studentToken);

        return (sessionId, tutorId, tutorToken, studentToken);
    }

    [Fact]
    public async Task StartLesson_creates_a_meeting_with_a_mock_join_and_host_url()
    {
        var (sessionId, _, tutorToken, _) = await BookOnlineSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/meeting", body: null, tutorToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(sessionId, body.GetProperty("value").GetProperty("sessionId").GetGuid());
        Assert.StartsWith("https://mock-meeting.tutorflow.dev/join/", body.GetProperty("value").GetProperty("joinUrl").GetString());
        Assert.StartsWith("https://mock-meeting.tutorflow.dev/host/", body.GetProperty("value").GetProperty("hostUrl").GetString());
    }

    [Fact]
    public async Task StartLesson_is_idempotent()
    {
        var (sessionId, _, tutorToken, _) = await BookOnlineSessionAsync();

        var first = await PostWithAuthAsync($"/sessions/{sessionId}/meeting", body: null, tutorToken);
        var second = await PostWithAuthAsync($"/sessions/{sessionId}/meeting", body: null, tutorToken);

        var firstId = (await ReadBodyAsync(first)).GetProperty("value").GetProperty("meetingId").GetGuid();
        var secondId = (await ReadBodyAsync(second)).GetProperty("value").GetProperty("meetingId").GetGuid();
        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public async Task StartLesson_is_forbidden_for_a_different_tutor()
    {
        var (sessionId, _, _, _) = await BookOnlineSessionAsync();
        var (_, otherTutorToken) = await RegisterAndLoginTutorAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/meeting", body: null, otherTutorToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StartLesson_requires_authentication()
    {
        var (sessionId, _, _, _) = await BookOnlineSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/meeting", body: null, bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StartLesson_rejects_an_in_person_session()
    {
        var (tutorId, tutorToken) = await RegisterAndLoginTutorAsync();
        var slotId = await DeclareInPersonAvailabilityAsync(tutorId, tutorToken);
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync();
        var sessionId = await BookSessionAsync(slotId, studentId, studentToken);

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/meeting", body: null, tutorToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("CreateMeetingCommand.SessionNotOnline", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetMeetingBySession_returns_not_found_before_any_meeting_has_been_started()
    {
        var (sessionId, _, tutorToken, _) = await BookOnlineSessionAsync();

        var response = await GetWithAuthAsync($"/sessions/{sessionId}/meeting", tutorToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMeetingBySession_returns_the_meeting_for_the_sessions_own_student()
    {
        var (sessionId, _, tutorToken, studentToken) = await BookOnlineSessionAsync();
        (await PostWithAuthAsync($"/sessions/{sessionId}/meeting", body: null, tutorToken)).EnsureSuccessStatusCode();

        var response = await GetWithAuthAsync($"/sessions/{sessionId}/meeting", studentToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetActiveMeetingForConversation_returns_the_meeting_once_started()
    {
        var (sessionId, tutorId, tutorToken, studentToken) = await BookOnlineSessionAsync();
        (await PostWithAuthAsync($"/sessions/{sessionId}/meeting", body: null, tutorToken)).EnsureSuccessStatusCode();

        var startConversation = await PostWithAuthAsync(
            "/conversations", new { TargetAccountId = tutorId }, studentToken);
        var conversationId = (await ReadBodyAsync(startConversation)).GetProperty("value").GetProperty("conversationId").GetGuid();

        var response = await GetWithAuthAsync($"/conversations/{conversationId}/active-meeting", studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.Equal(sessionId, body.GetProperty("value").GetProperty("sessionId").GetGuid());
    }

    [Fact]
    public async Task GetActiveMeetingForConversation_returns_null_when_no_meeting_has_been_started()
    {
        var (_, tutorId, _, studentToken) = await BookOnlineSessionAsync();

        var startConversation = await PostWithAuthAsync(
            "/conversations", new { TargetAccountId = tutorId }, studentToken);
        var conversationId = (await ReadBodyAsync(startConversation)).GetProperty("value").GetProperty("conversationId").GetGuid();

        var response = await GetWithAuthAsync($"/conversations/{conversationId}/active-meeting", studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("value").ValueKind);
    }
}
