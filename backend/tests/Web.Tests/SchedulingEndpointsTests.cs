using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Web.Tests;

// End-to-end tests against the real HTTP pipeline — see IdentityEndpointsTests
// for the scope discipline these follow (Presentation orchestration only).
// DeclareAvailability now also exercises AuthorizationMiddleware's real
// enforcement (WP4 Priority 3, AUTHORIZATION_MATRIX.md §4.3).
public class SchedulingEndpointsTests : IClassFixture<TutorFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly TutorFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SchedulingEndpointsTests(TutorFlowWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    // Mirrors IdentityEndpointsTests.SeedAndLoginAdminAsync exactly — see its
    // own comment for why seeding, not registration, is the correct
    // mechanism (ADR-017: Admin/Staff is manually provisioned only).
    private async Task<string> SeedAndLoginAdminAsync()
    {
        var email = UniqueEmail();

        using (var scope = _factory.Services.CreateScope())
        {
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var adminRepository = scope.ServiceProvider.GetRequiredService<IAdminStaffRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var admin = AdminStaff.Create(EmailAddress.Of(email), PasswordHash.Of(passwordHasher.Hash(TestPassword)));
            await adminRepository.AddAsync(admin);
            await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { admin });
        }

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        var loginBody = await ReadBodyAsync(loginResponse);
        return loginBody.GetProperty("value").GetProperty("token").GetString()!;
    }

    // RegisterTutorCommand now requires Email/Password (Account credentials
    // are mandatory); the shared in-memory SQLite database enforces a UNIQUE
    // index on Email, so each registration needs its own distinct address.
    private static string UniqueEmail() => $"test-{Guid.NewGuid():N}@example.com";

    private const string TestPassword = "Test-Password-123!";

    private async Task<(Guid TutorId, string Token)> RegisterAndLoginTutorAsync()
    {
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = email, Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var tutorId = registerBody.GetProperty("value").GetProperty("tutorId").GetGuid();

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        var loginBody = await ReadBodyAsync(loginResponse);
        return (tutorId, loginBody.GetProperty("value").GetProperty("token").GetString()!);
    }

    // An adult Student is the simplest valid BookSession caller (IDR-5) — no
    // confirmed Relationship needed, unlike a Parent/Guardian caller.
    private async Task<(Guid StudentId, string Token)> RegisterAndLoginStudentAsync()
    {
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync(
            "/students", new { Email = email, Password = TestPassword, IsMinor = false });
        var registerBody = await ReadBodyAsync(registerResponse);
        var studentId = registerBody.GetProperty("value").GetProperty("studentId").GetGuid();

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        var loginBody = await ReadBodyAsync(loginResponse);
        return (studentId, loginBody.GetProperty("value").GetProperty("token").GetString()!);
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

    private async Task<Guid> DeclareAvailabilityAsync()
    {
        var (slotId, _) = await DeclareAvailabilityWithTutorAsync();
        return slotId;
    }

    private async Task<(Guid SlotId, string TutorToken)> DeclareAvailabilityWithTutorAsync()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();
        var response = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0, // Online
        }, token);
        var body = await ReadBodyAsync(response);
        return (body.GetProperty("value").GetProperty("availabilitySlotId").GetGuid(), token);
    }

    private async Task<(Guid SessionId, string StudentToken, string TutorToken)> BookSessionAsync()
    {
        var (slotId, tutorToken) = await DeclareAvailabilityWithTutorAsync();
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync();
        var response = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = slotId,
            StudentId = studentId,
            ParentGuardianId = (Guid?)null,
        }, studentToken);
        var body = await ReadBodyAsync(response);
        return (body.GetProperty("value").GetProperty("sessionId").GetGuid(), studentToken, tutorToken);
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

    private async Task<Guid> DeclareAvailabilityForTutorAsync(Guid tutorId, string token)
    {
        var response = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0, // Online
        }, token);
        var body = await ReadBodyAsync(response);
        return body.GetProperty("value").GetProperty("availabilitySlotId").GetGuid();
    }

    [Fact]
    public async Task DeclareAvailability_is_reachable_and_maps_request_correctly()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0,
        }, token);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(tutorId, body.GetProperty("value").GetProperty("tutorId").GetGuid());
    }

    [Fact]
    public async Task DeclareAvailability_returns_failure_for_empty_tutor_id()
    {
        var (_, token) = await RegisterAndLoginTutorAsync();

        var response = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = Guid.Empty,
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0,
        }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    [Fact]
    public async Task DeclareAvailability_requires_authentication()
    {
        var response = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = Guid.NewGuid(),
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0,
        }, bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    // Layer 2 (ownership) — the caller here IS a Tutor (passes
    // RequirePermission(DeclareAvailability) fine) but claims a *different*
    // Tutor's id in the request body; DeclareAvailability has no
    // pre-existing resource to load (unlike ManageTutorOffering), so this
    // exercises the "never trust the client-supplied TutorId" check on its
    // own, distinct shape (WP4 Priority 3 Layer 2).
    [Fact]
    public async Task DeclareAvailability_is_forbidden_when_TutorId_does_not_match_the_caller()
    {
        var (otherTutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, callerToken) = await RegisterAndLoginTutorAsync();

        var response = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = otherTutorId,
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0,
        }, callerToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("DeclareAvailabilityCommand.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task BookSession_returns_success_for_declared_slot()
    {
        var slotId = await DeclareAvailabilityAsync();
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = slotId,
            StudentId = studentId,
            ParentGuardianId = (Guid?)null,
        }, studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(slotId, body.GetProperty("value").GetProperty("availabilitySlotId").GetGuid());
    }

    [Fact]
    public async Task BookSession_returns_failure_for_unknown_slot()
    {
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = Guid.NewGuid(),
            StudentId = studentId,
            ParentGuardianId = (Guid?)null,
        }, studentToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    [Fact]
    public async Task BookSession_requires_authentication()
    {
        var slotId = await DeclareAvailabilityAsync();

        var response = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = slotId,
            StudentId = Guid.NewGuid(),
            ParentGuardianId = (Guid?)null,
        }, bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task BookSession_is_forbidden_when_caller_is_not_the_named_student()
    {
        var slotId = await DeclareAvailabilityAsync();
        var (_, otherStudentToken) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = slotId,
            StudentId = Guid.NewGuid(),
            ParentGuardianId = (Guid?)null,
        }, otherStudentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("BookSessionCommand.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task RescheduleSession_returns_success_for_scheduled_session()
    {
        var (sessionId, studentToken, _) = await BookSessionAsync();
        var newTime = DateTime.UtcNow.AddDays(2);

        var response = await PostWithAuthAsync(
            $"/sessions/{sessionId}/reschedule", new { NewScheduledTimeUtc = newTime }, studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task RescheduleSession_returns_failure_for_unknown_session()
    {
        var (_, token) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync(
            $"/sessions/{Guid.NewGuid()}/reschedule", new { NewScheduledTimeUtc = DateTime.UtcNow.AddDays(2) }, token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    [Fact]
    public async Task RescheduleSession_is_forbidden_for_a_non_party_caller()
    {
        var (sessionId, _, _) = await BookSessionAsync();
        var (_, otherStudentToken) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync(
            $"/sessions/{sessionId}/reschedule", new { NewScheduledTimeUtc = DateTime.UtcNow.AddDays(2) }, otherStudentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("RescheduleSessionCommand.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CancelSession_returns_success_for_scheduled_session()
    {
        var (sessionId, studentToken, _) = await BookSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/cancel", body: null, studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task CancelSession_returns_failure_for_unknown_session()
    {
        var (_, token) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync($"/sessions/{Guid.NewGuid()}/cancel", body: null, token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    [Fact]
    public async Task CancelSession_is_forbidden_for_a_non_party_caller()
    {
        var (sessionId, _, _) = await BookSessionAsync();
        var (_, otherStudentToken) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/cancel", body: null, otherStudentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("CancelSessionCommand.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CompleteSession_returns_success_for_scheduled_session()
    {
        var (sessionId, _, tutorToken) = await BookSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/complete", body: null, tutorToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task CompleteSession_returns_failure_for_unknown_session()
    {
        var (_, tutorToken) = await RegisterAndLoginTutorAsync();

        var response = await PostWithAuthAsync($"/sessions/{Guid.NewGuid()}/complete", body: null, tutorToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    [Fact]
    public async Task CompleteSession_requires_authentication()
    {
        var (sessionId, _, _) = await BookSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/complete", body: null, bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CompleteSession_is_forbidden_for_a_different_tutor()
    {
        var (sessionId, _, _) = await BookSessionAsync();
        var (_, otherTutorToken) = await RegisterAndLoginTutorAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/complete", body: null, otherTutorToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("CompleteSessionCommand.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CompleteSession_is_forbidden_for_the_booking_student()
    {
        var (sessionId, studentToken, _) = await BookSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/complete", body: null, studentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MarkSessionNoShow_returns_success_for_scheduled_session()
    {
        var (sessionId, _, tutorToken) = await BookSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/no-show", body: null, tutorToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task MarkSessionNoShow_is_forbidden_for_a_different_tutor()
    {
        var (sessionId, _, _) = await BookSessionAsync();
        var (_, otherTutorToken) = await RegisterAndLoginTutorAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/no-show", body: null, otherTutorToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("MarkSessionNoShowCommand.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task MarkSessionNoShow_returns_failure_for_unknown_session()
    {
        var (_, tutorToken) = await RegisterAndLoginTutorAsync();

        var response = await PostWithAuthAsync($"/sessions/{Guid.NewGuid()}/no-show", body: null, tutorToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    [Fact]
    public async Task MarkSessionNoShow_requires_authentication()
    {
        var (sessionId, _, _) = await BookSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/no-show", body: null, bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetTutorAvailabilitySlots_includes_a_freshly_declared_slot()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();
        var slotId = await DeclareAvailabilityForTutorAsync(tutorId, token);

        var response = await GetWithAuthAsync($"/tutors/{tutorId}/availability-slots", token);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var ids = body.GetProperty("value").EnumerateArray().Select(s => s.GetProperty("availabilitySlotId").GetGuid());
        Assert.Contains(slotId, ids);
    }

    [Fact]
    public async Task GetTutorAvailabilitySlots_reflects_consumption_state_once_booked()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();
        var slotId = await DeclareAvailabilityForTutorAsync(tutorId, token);
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync();
        await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = slotId,
            StudentId = studentId,
            ParentGuardianId = (Guid?)null,
        }, studentToken);

        var response = await GetWithAuthAsync($"/tutors/{tutorId}/availability-slots", token);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var slot = body.GetProperty("value").EnumerateArray().Single(s => s.GetProperty("availabilitySlotId").GetGuid() == slotId);
        Assert.True(slot.GetProperty("isConsumed").GetBoolean());
    }

    [Fact]
    public async Task GetTutorAvailabilitySlots_returns_success_for_any_authenticated_caller_when_discoverable()
    {
        var (tutorId, tutorToken) = await RegisterAndLoginTutorAsync();
        var adminToken = await SeedAndLoginAdminAsync();
        await PostWithAuthAsync($"/tutors/{tutorId}/approve", body: null, adminToken);
        var (_, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/tutors/{tutorId}/availability-slots", studentToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetTutorAvailabilitySlots_requires_authentication()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await GetWithAuthAsync($"/tutors/{tutorId}/availability-slots", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTutorAvailabilitySlots_is_forbidden_for_an_unrelated_caller_when_not_discoverable()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/tutors/{tutorId}/availability-slots", studentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTutorAvailabilitySlots_returns_not_found_for_unregistered_tutor()
    {
        var response = await GetWithAuthAsync($"/tutors/{Guid.NewGuid()}/availability-slots", bearerToken: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAvailabilitySlotById_returns_success_for_the_declaring_tutor()
    {
        var (slotId, tutorToken) = await DeclareAvailabilityWithTutorAsync();

        var response = await GetWithAuthAsync($"/availability-slots/{slotId}", tutorToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.Equal(slotId, body.GetProperty("value").GetProperty("availabilitySlotId").GetGuid());
    }

    [Fact]
    public async Task GetAvailabilitySlotById_returns_success_for_the_booking_student_once_consumed()
    {
        var (sessionId, studentToken, _) = await BookSessionAsync();
        var sessionResponse = await GetWithAuthAsync($"/sessions/{sessionId}", studentToken);
        var slotId = (await ReadBodyAsync(sessionResponse)).GetProperty("value").GetProperty("availabilitySlotId").GetGuid();

        var response = await GetWithAuthAsync($"/availability-slots/{slotId}", studentToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAvailabilitySlotById_requires_authentication()
    {
        var slotId = await DeclareAvailabilityAsync();

        var response = await GetWithAuthAsync($"/availability-slots/{slotId}", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAvailabilitySlotById_is_forbidden_for_an_unrelated_caller()
    {
        var slotId = await DeclareAvailabilityAsync();
        var (_, otherStudentToken) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/availability-slots/{slotId}", otherStudentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("GetAvailabilitySlotByIdQuery.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetAvailabilitySlotById_returns_failure_for_unknown_slot()
    {
        var (_, token) = await RegisterAndLoginTutorAsync();

        var response = await GetWithAuthAsync($"/availability-slots/{Guid.NewGuid()}", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSessionById_returns_success_for_the_booking_student()
    {
        var (sessionId, studentToken, _) = await BookSessionAsync();

        var response = await GetWithAuthAsync($"/sessions/{sessionId}", studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.Equal(sessionId, body.GetProperty("value").GetProperty("sessionId").GetGuid());
    }

    [Fact]
    public async Task GetSessionById_returns_success_for_the_tutor()
    {
        var (sessionId, _, tutorToken) = await BookSessionAsync();

        var response = await GetWithAuthAsync($"/sessions/{sessionId}", tutorToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetSessionById_requires_authentication()
    {
        var (sessionId, _, _) = await BookSessionAsync();

        var response = await GetWithAuthAsync($"/sessions/{sessionId}", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSessionById_is_forbidden_for_a_non_party_caller()
    {
        var (sessionId, _, _) = await BookSessionAsync();
        var (_, otherStudentToken) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/sessions/{sessionId}", otherStudentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("GetSessionByIdQuery.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetSessionById_returns_failure_for_unknown_session()
    {
        var (_, token) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/sessions/{Guid.NewGuid()}", token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentSchedule_returns_success_for_the_student_themselves()
    {
        var (sessionId, studentToken, _) = await BookSessionAsync();
        var studentIdResponse = await GetWithAuthAsync($"/sessions/{sessionId}", studentToken);
        var studentId = (await ReadBodyAsync(studentIdResponse)).GetProperty("value").GetProperty("studentId").GetGuid();

        var response = await GetWithAuthAsync($"/students/{studentId}/schedule", studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var ids = body.GetProperty("value").EnumerateArray().Select(s => s.GetProperty("sessionId").GetGuid());
        Assert.Contains(sessionId, ids);
    }

    [Fact]
    public async Task GetStudentSchedule_returns_success_for_a_parent_guardian_with_a_confirmed_relationship()
    {
        var studentEmail = UniqueEmail();
        var studentRegisterResponse = await _client.PostAsJsonAsync(
            "/students", new { Email = studentEmail, Password = TestPassword, IsMinor = false });
        var studentId = (await ReadBodyAsync(studentRegisterResponse)).GetProperty("value").GetProperty("studentId").GetGuid();
        var studentLoginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = studentEmail, Password = TestPassword });
        var studentToken = (await ReadBodyAsync(studentLoginResponse)).GetProperty("value").GetProperty("token").GetString()!;

        var parentGuardianEmail = UniqueEmail();
        var parentGuardianRegisterResponse = await _client.PostAsJsonAsync(
            "/parent-guardians", new { Email = parentGuardianEmail, Password = TestPassword });
        var parentGuardianId = (await ReadBodyAsync(parentGuardianRegisterResponse)).GetProperty("value").GetProperty("parentGuardianId").GetGuid();
        var parentGuardianLoginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = parentGuardianEmail, Password = TestPassword });
        var parentGuardianToken = (await ReadBodyAsync(parentGuardianLoginResponse)).GetProperty("value").GetProperty("token").GetString()!;

        var inviteResponse = await PostWithAuthAsync(
            "/relationships", new { ParentGuardianId = parentGuardianId, StudentId = studentId }, parentGuardianToken);
        var relationshipId = (await ReadBodyAsync(inviteResponse)).GetProperty("value").GetProperty("relationshipId").GetGuid();
        await PostWithAuthAsync($"/relationships/{relationshipId}/confirm", body: null, studentToken);

        var response = await GetWithAuthAsync($"/students/{studentId}/schedule", parentGuardianToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetStudentSchedule_returns_success_for_admin()
    {
        var (studentId, _) = await RegisterAndLoginStudentAsync();
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await GetWithAuthAsync($"/students/{studentId}/schedule", adminToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetStudentSchedule_requires_authentication()
    {
        var (studentId, _) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/students/{studentId}/schedule", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentSchedule_is_forbidden_for_an_unrelated_caller()
    {
        var (studentId, _) = await RegisterAndLoginStudentAsync();
        var (_, otherToken) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/students/{studentId}/schedule", otherToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentSchedule_returns_failure_for_unknown_student()
    {
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await GetWithAuthAsync($"/students/{Guid.NewGuid()}/schedule", adminToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTutorSchedule_returns_success_for_the_tutor_themselves()
    {
        var (sessionId, _, tutorToken) = await BookSessionAsync();
        var sessionResponse = await GetWithAuthAsync($"/sessions/{sessionId}", tutorToken);
        var tutorId = (await ReadBodyAsync(sessionResponse)).GetProperty("value").GetProperty("tutorId").GetGuid();

        var response = await GetWithAuthAsync($"/tutors/{tutorId}/schedule", tutorToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var ids = body.GetProperty("value").EnumerateArray().Select(s => s.GetProperty("sessionId").GetGuid());
        Assert.Contains(sessionId, ids);
    }

    [Fact]
    public async Task GetTutorSchedule_returns_success_for_admin()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await GetWithAuthAsync($"/tutors/{tutorId}/schedule", adminToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetTutorSchedule_requires_authentication()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();

        var response = await GetWithAuthAsync($"/tutors/{tutorId}/schedule", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTutorSchedule_is_forbidden_for_a_different_tutor()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, otherTutorToken) = await RegisterAndLoginTutorAsync();

        var response = await GetWithAuthAsync($"/tutors/{tutorId}/schedule", otherTutorToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTutorSchedule_returns_failure_for_unknown_tutor()
    {
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await GetWithAuthAsync($"/tutors/{Guid.NewGuid()}/schedule", adminToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
