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

    // Sends a literal, hand-written JSON body rather than letting
    // System.Text.Json serialize a DateTime object — needed to construct
    // timestamps System.Text.Json itself would never produce (e.g. missing
    // "Z"), which is exactly what RequireUtcDateTimeJsonConverter must reject.
    private async Task<HttpResponseMessage> PostRawJsonWithAuthAsync(string url, string rawJsonBody, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(rawJsonBody, System.Text.Encoding.UTF8, "application/json"),
        };
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

    // startTimeUtc is overridable (defaults to now+1 day) so a caller
    // declaring a second slot for the same Tutor (e.g.
    // BookSessionWithASecondOpenSlotAsync) can pick a genuinely
    // non-overlapping time — Phase 8a's overlap guard on
    // AvailabilitySlot.Declare would otherwise reject a second call at the
    // same default start time as a real conflict, which is correct
    // production behavior, not a test bug to work around silently.
    private async Task<Guid> DeclareAvailabilityForTutorAsync(Guid tutorId, string token, DateTime? startTimeUtc = null)
    {
        var response = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = startTimeUtc ?? DateTime.UtcNow.AddDays(1),
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

    // Phase 8a: closes the gap BookSessionCommandHandler's own comment
    // flagged as "explicitly deferred to a later phase" — declaring a
    // second slot that overlaps an already-declared one for the same
    // Tutor is rejected, at the same 409 Conflict/InvalidOperationException
    // shape the existing IsConsumed guard above already established for
    // this Domain error family. Verified end-to-end that no second row was
    // persisted — GET the Tutor's slots from a fresh request afterward and
    // confirm only the original slot exists (CLAUDE.md's own re-read rule).
    [Fact]
    public async Task DeclareAvailability_rejects_a_slot_that_overlaps_one_already_declared_by_the_same_tutor()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();
        var start = DateTime.UtcNow.AddDays(1);
        var firstResponse = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = start,
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0,
        }, token);
        firstResponse.EnsureSuccessStatusCode();

        var overlapResponse = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = start.AddMinutes(30),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0,
        }, token);

        Assert.Equal(HttpStatusCode.Conflict, overlapResponse.StatusCode);
        var overlapBody = await ReadBodyAsync(overlapResponse);
        Assert.True(overlapBody.GetProperty("isFailure").GetBoolean());
        Assert.Equal(
            "DeclareAvailabilityCommand.InvalidState",
            overlapBody.GetProperty("error").GetProperty("code").GetString());

        var slotsResponse = await GetWithAuthAsync($"/tutors/{tutorId}/availability-slots", token);
        var slotsBody = await ReadBodyAsync(slotsResponse);
        Assert.Equal(1, slotsBody.GetProperty("value").GetArrayLength());
    }

    [Fact]
    public async Task DeclareAvailability_accepts_a_back_to_back_slot_that_only_touches_an_existing_one()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();
        var start = DateTime.UtcNow.AddDays(1);
        var firstResponse = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = start,
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0,
        }, token);
        firstResponse.EnsureSuccessStatusCode();

        var secondResponse = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = start.AddHours(1),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0,
        }, token);

        secondResponse.EnsureSuccessStatusCode();
    }

    // Phase 3 Task 4: the API boundary must reject any incoming timestamp
    // lacking an explicit UTC designator ("Z") or numeric offset, rather
    // than silently treating it as UTC (which for a Tehran caller would be
    // a silent 3.5-hour error). Sent as a hand-written raw JSON body since
    // System.Text.Json would never itself produce a "Z"-less string.
    [Fact]
    public async Task DeclareAvailability_rejects_a_start_time_without_an_explicit_utc_designator()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PostRawJsonWithAuthAsync(
            "/availability-slots",
            $$"""{"TutorId":"{{tutorId}}","StartTimeUtc":"2026-08-01T14:00:00","Duration":"01:00:00","DeliveryMode":0}""",
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // The companion acceptance path: a non-"Z" but still explicit numeric
    // offset must be accepted and converted to the *correct* UTC instant —
    // not resolved against this test process's own system timezone (the
    // System.Text.Json default converter's Kind=Local bug this replaces).
    [Fact]
    public async Task DeclareAvailability_accepts_a_start_time_with_an_explicit_non_zulu_offset()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        // 2026-08-01T17:30:00+03:30 is the same instant as 2026-08-01T14:00:00Z.
        var response = await PostRawJsonWithAuthAsync(
            "/availability-slots",
            $$"""{"TutorId":"{{tutorId}}","StartTimeUtc":"2026-08-01T17:30:00+03:30","Duration":"01:00:00","DeliveryMode":0}""",
            token);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.Equal(
            DateTime.Parse("2026-08-01T14:00:00Z").ToUniversalTime(),
            body.GetProperty("value").GetProperty("startTimeUtc").GetDateTime().ToUniversalTime());
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

    // Phase 4.7: books Session onto slot A for a given Tutor, then declares
    // a second, still-open slot B for that SAME Tutor — the shape every
    // Reschedule test below needs (the new endpoint targets a slot, not a
    // raw timestamp, so it must belong to the Session's own Tutor).
    private async Task<(Guid SessionId, string StudentToken, Guid OldSlotId, Guid NewSlotId)> BookSessionWithASecondOpenSlotAsync()
    {
        var (tutorId, tutorToken) = await RegisterAndLoginTutorAsync();
        var oldSlotId = await DeclareAvailabilityForTutorAsync(tutorId, tutorToken);
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync();
        var bookResponse = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = oldSlotId,
            StudentId = studentId,
            ParentGuardianId = (Guid?)null,
        }, studentToken);
        var bookBody = await ReadBodyAsync(bookResponse);
        var sessionId = bookBody.GetProperty("value").GetProperty("sessionId").GetGuid();

        var newSlotId = await DeclareAvailabilityForTutorAsync(tutorId, tutorToken, DateTime.UtcNow.AddDays(2));

        return (sessionId, studentToken, oldSlotId, newSlotId);
    }

    [Fact]
    public async Task RescheduleSession_returns_success_for_scheduled_session()
    {
        var (sessionId, studentToken, _, newSlotId) = await BookSessionWithASecondOpenSlotAsync();

        var response = await PostWithAuthAsync(
            $"/sessions/{sessionId}/reschedule", new { NewAvailabilitySlotId = newSlotId }, studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task RescheduleSession_returns_failure_for_unknown_session()
    {
        var (_, token) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync(
            $"/sessions/{Guid.NewGuid()}/reschedule", new { NewAvailabilitySlotId = Guid.NewGuid() }, token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    // Phase 4.7 Task 3: replaces the old "rejects a Z-less timestamp" test
    // — the contract no longer accepts a raw timestamp at all, so the
    // analogous boundary case is now an unknown target slot id.
    [Fact]
    public async Task RescheduleSession_returns_failure_for_unknown_availability_slot()
    {
        var (sessionId, studentToken, _) = await BookSessionAsync();

        var response = await PostWithAuthAsync(
            $"/sessions/{sessionId}/reschedule", new { NewAvailabilitySlotId = Guid.NewGuid() }, studentToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal(
            "RescheduleSessionCommand.AvailabilitySlotNotFound",
            body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task RescheduleSession_is_forbidden_for_a_non_party_caller()
    {
        var (sessionId, _, _) = await BookSessionAsync();
        var (_, otherStudentToken) = await RegisterAndLoginStudentAsync();

        var response = await PostWithAuthAsync(
            $"/sessions/{sessionId}/reschedule", new { NewAvailabilitySlotId = Guid.NewGuid() }, otherStudentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("RescheduleSessionCommand.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    // Phase 4.7 Task 1: proved today's defect before any fix, mirroring
    // Phase 4.6's identical precedent for Cancel
    // (CancelSession_reopens_the_slot_so_it_can_be_rebooked, d379619): once
    // a Session moves off an AvailabilitySlot, that slot should become
    // rebookable again — the same guarantee Cancel already provides. This
    // test originally called the old raw-timestamp Reschedule endpoint and
    // failed with 409 (the old slot stayed permanently IsConsumed). Task 3
    // updated its request body to the new NewAvailabilitySlotId contract —
    // the assertions themselves (rebooking the vacated slot must succeed)
    // are unchanged and now pass, proving Reschedule really does release
    // the old slot via AvailabilitySlot.Reopen().
    [Fact]
    public async Task RescheduleSession_reopens_the_old_slot_so_it_can_be_rebooked()
    {
        var (sessionId, studentToken, oldSlotId, newSlotId) = await BookSessionWithASecondOpenSlotAsync();

        var rescheduleResponse = await PostWithAuthAsync(
            $"/sessions/{sessionId}/reschedule",
            new { NewAvailabilitySlotId = newSlotId },
            studentToken);
        rescheduleResponse.EnsureSuccessStatusCode();

        var (otherStudentId, otherStudentToken) = await RegisterAndLoginStudentAsync();
        var rebookResponse = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = oldSlotId,
            StudentId = otherStudentId,
            ParentGuardianId = (Guid?)null,
        }, otherStudentToken);

        rebookResponse.EnsureSuccessStatusCode();
        var rebookBody = await ReadBodyAsync(rebookResponse);
        Assert.True(rebookBody.GetProperty("isSuccess").GetBoolean());
        Assert.NotEqual(sessionId, rebookBody.GetProperty("value").GetProperty("sessionId").GetGuid());
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

    // Phase 4.6 (a live defect Phase 5A's study surfaced, independent of
    // payments): AvailabilitySlot.IsConsumed is never reset by
    // Session.Cancel(), and the unique index backing CONST-1
    // (SessionConfiguration: HasIndex(s => s.AvailabilitySlotId).IsUnique())
    // is unconditional, not filtered by Status. A cancelled Session
    // therefore blocks its slot permanently today - proven here before any
    // fix, per the Phase 1B precedent of committing a red test first.
    //
    // Confirmed by direct observation (not inferred) which layer rejects
    // the rebooking attempt: the response is 409 Conflict,
    // "BookSessionCommand.InvalidState", "This Availability Slot has
    // already been consumed and cannot produce another Session." - the
    // exact message AvailabilitySlot.Book()'s in-memory `if (IsConsumed)
    // throw` guard raises. This is the Domain-level IsConsumed check, not
    // the database unique index: a ConcurrencyConflictException (the
    // index's own failure mode) would instead have produced
    // "BookSessionCommand.SlotAlreadyBooked" (see
    // BookSessionCommandHandler's two distinct catch clauses). The second
    // booking attempt never reaches the database at all in this scenario -
    // it is rejected in memory before any SQL is issued.
    [Fact]
    public async Task CancelSession_reopens_the_slot_so_it_can_be_rebooked()
    {
        var (slotId, _) = await DeclareAvailabilityWithTutorAsync();
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync();
        var bookResponse = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = slotId,
            StudentId = studentId,
            ParentGuardianId = (Guid?)null,
        }, studentToken);
        var bookBody = await ReadBodyAsync(bookResponse);
        var sessionId = bookBody.GetProperty("value").GetProperty("sessionId").GetGuid();

        var cancelResponse = await PostWithAuthAsync($"/sessions/{sessionId}/cancel", body: null, studentToken);
        cancelResponse.EnsureSuccessStatusCode();

        var (otherStudentId, otherStudentToken) = await RegisterAndLoginStudentAsync();
        var rebookResponse = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = slotId,
            StudentId = otherStudentId,
            ParentGuardianId = (Guid?)null,
        }, otherStudentToken);

        rebookResponse.EnsureSuccessStatusCode();
        var rebookBody = await ReadBodyAsync(rebookResponse);
        Assert.True(rebookBody.GetProperty("isSuccess").GetBoolean());
        Assert.NotEqual(sessionId, rebookBody.GetProperty("value").GetProperty("sessionId").GetGuid());
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
