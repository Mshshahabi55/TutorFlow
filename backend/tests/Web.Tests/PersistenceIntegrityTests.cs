using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Web.Tests;

// PHASE-01B: these tests exist specifically to prove or disprove persistence
// for every COMMAND PATH repository method found in the Task 1 audit
// (docs/phases/PHASE-01B-REPORT.md Section 3) — one dedicated test per
// method, each performing the mutation via the real HTTP pipeline and then
// re-reading the entity from a brand new DbContext scope, never from the
// HTTP response body and never from a context that could still be holding
// the entity in its first-level (identity map) cache. A test that passes
// here found a repository method that was already correctly tracked; a test
// that fails found one still using .AsNoTracking() on its command path,
// per the same root cause Phase 1 first identified in ApproveTutor.
public class PersistenceIntegrityTests : IClassFixture<TutorFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly TutorFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PersistenceIntegrityTests(TutorFlowWebApplicationFactory factory)
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

    // Every re-read in this file goes through a brand-new DbContext instance
    // from a brand-new DI scope — never the one the request pipeline used —
    // so a passing assertion can only mean the row is actually in the
    // database, not that EF's identity map handed back the same in-memory
    // instance the handler just mutated.
    private TutorFlowDbContext FreshDbContext(out IServiceScope scope)
    {
        scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TutorFlowDbContext>();
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

    private async Task<HttpResponseMessage> PatchWithAuthAsync(string url, object body, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = JsonContent.Create(body) };
        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await _client.SendAsync(request);
    }

    private async Task<string> SeedAndLoginAdminAsync()
    {
        var email = UniqueEmail("admin");

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

    private async Task<(Guid StudentId, string Token, string Email)> RegisterAndLoginStudentAsync()
    {
        var email = UniqueEmail("student");
        var registerResponse = await _client.PostAsJsonAsync(
            "/students", new { Email = email, Password = TestPassword, IsMinor = false });
        var registerBody = await ReadBodyAsync(registerResponse);
        var studentId = registerBody.GetProperty("value").GetProperty("studentId").GetGuid();

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        var loginBody = await ReadBodyAsync(loginResponse);
        return (studentId, loginBody.GetProperty("value").GetProperty("token").GetString()!, email);
    }

    private async Task<(Guid SlotId, Guid TutorId, string TutorToken)> DeclareAvailabilityAsync()
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
        return (body.GetProperty("value").GetProperty("availabilitySlotId").GetGuid(), tutorId, token);
    }

    private async Task<(Guid SessionId, string StudentToken, string TutorToken, Guid SlotId, Guid TutorId)> BookSessionAsync()
    {
        var (slotId, tutorId, tutorToken) = await DeclareAvailabilityAsync();
        var (studentId, studentToken, _) = await RegisterAndLoginStudentAsync();
        var response = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = slotId,
            StudentId = studentId,
            ParentGuardianId = (Guid?)null,
        }, studentToken);
        var body = await ReadBodyAsync(response);
        return (body.GetProperty("value").GetProperty("sessionId").GetGuid(), studentToken, tutorToken, slotId, tutorId);
    }

    // --- Tutor-repository command paths (TutorRepository.GetByIdAsync / GetByEmailAsync — AsNoTracking, per the Task 1 audit) ---

    [Fact]
    public async Task ApproveTutor_persists_IsApproved_when_reread_from_a_fresh_scope()
    {
        var adminToken = await SeedAndLoginAdminAsync();
        var (tutorId, _) = await RegisterAndLoginTutorAsync();

        var response = await PostWithAuthAsync($"/tutors/{tutorId}/approve", body: null, adminToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.True(freshTutor.IsApproved);
        }
    }

    [Fact]
    public async Task SuspendTutor_persists_IsSuspended_when_reread_from_a_fresh_scope()
    {
        var adminToken = await SeedAndLoginAdminAsync();
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        (await PostWithAuthAsync($"/tutors/{tutorId}/approve", body: null, adminToken)).EnsureSuccessStatusCode();

        var response = await PostWithAuthAsync($"/tutors/{tutorId}/suspend", body: null, adminToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.True(freshTutor.IsSuspended);
        }
    }

    [Fact]
    public async Task SetTutorHourlyRate_persists_the_new_rate_when_reread_from_a_fresh_scope()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/hourly-rate", new { Amount = 250000m }, token);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.NotNull(freshTutor.HourlyRate);
            Assert.Equal(250000m, freshTutor.HourlyRate!.Amount);
        }
    }

    [Fact]
    public async Task SetTutorSubject_persists_the_new_subject_when_reread_from_a_fresh_scope()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/subject", new { Subject = "Mathematics" }, token);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.Equal("Mathematics", freshTutor.Subject?.Value);
        }
    }

    [Fact]
    public async Task SetTutorLanguage_persists_the_new_language_when_reread_from_a_fresh_scope()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/language", new { Language = "Persian" }, token);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.Equal("Persian", freshTutor.Language?.Value);
        }
    }

    [Fact]
    public async Task SetTutorLocation_persists_the_new_location_when_reread_from_a_fresh_scope()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/location", new { Location = "Tehran" }, token);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.Equal("Tehran", freshTutor.Location?.Value);
        }
    }

    [Fact]
    public async Task SetTutorOfferedDurations_persists_the_new_durations_when_reread_from_a_fresh_scope()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync(
            $"/tutors/{tutorId}/offered-durations",
            new { Durations = new[] { "00:30:00", "01:00:00" } },
            token);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.Equal(
                new[] { TimeSpan.FromMinutes(30), TimeSpan.FromHours(1) },
                freshTutor.OfferedDurations.OrderBy(d => d));
        }
    }

    // --- Already-tracked command paths (Student/ParentGuardian/AdminStaff/AuthToken/Relationship/Session/AvailabilitySlot repositories) ---
    // Task 2 requires reporting explicitly when these pass, since a passing
    // test here proves a repository method the Task 1 audit found was
    // already correct, not one this phase's fix needs to touch.

    [Fact]
    public async Task AdminResetPassword_persists_the_new_password_for_a_Student_target()
    {
        var adminToken = await SeedAndLoginAdminAsync();
        var (studentId, _, _) = await RegisterAndLoginStudentAsync();
        const string newPassword = "Brand-New-Password-456!";

        var response = await PostWithAuthAsync(
            $"/auth/accounts/{studentId}/reset-password", new { NewPassword = newPassword }, adminToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var freshStudent = await dbContext.Students.AsNoTracking().FirstAsync(s => s.Id == AccountId.From(studentId));
            Assert.True(passwordHasher.Verify(newPassword, freshStudent.PasswordHash.Value));
        }
    }

    [Fact]
    public async Task Logout_persists_the_AuthToken_revocation_when_reread_from_a_fresh_scope()
    {
        var (_, token) = await RegisterAndLoginTutorAsync();

        var response = await _client.PostAsJsonAsync("/auth/logout", new { Token = token });
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshToken = await dbContext.AuthTokens.AsNoTracking().FirstAsync();
            Assert.NotNull(freshToken.RevokedAtUtc);
        }
    }

    [Fact]
    public async Task ConfirmRelationship_persists_Confirmed_status_when_reread_from_a_fresh_scope()
    {
        var parentEmail = UniqueEmail("parent");
        var parentRegisterResponse = await _client.PostAsJsonAsync(
            "/parent-guardians", new { Email = parentEmail, Password = TestPassword });
        var parentGuardianId = (await ReadBodyAsync(parentRegisterResponse))
            .GetProperty("value").GetProperty("parentGuardianId").GetGuid();
        var parentLoginBody = await ReadBodyAsync(
            await _client.PostAsJsonAsync("/auth/login", new { Email = parentEmail, Password = TestPassword }));
        var parentToken = parentLoginBody.GetProperty("value").GetProperty("token").GetString();

        var (studentId, studentToken, _) = await RegisterAndLoginStudentAsync();

        var inviteResponse = await PostWithAuthAsync(
            "/relationships", new { ParentGuardianId = parentGuardianId, StudentId = studentId }, parentToken);
        var relationshipId = (await ReadBodyAsync(inviteResponse)).GetProperty("value").GetProperty("relationshipId").GetGuid();

        var confirmResponse = await PostWithAuthAsync($"/relationships/{relationshipId}/confirm", body: null, studentToken);
        confirmResponse.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshRelationship = await dbContext.Relationships.AsNoTracking()
                .FirstAsync(r => r.Id == RelationshipId.From(relationshipId));
            Assert.Equal(RelationshipStatus.Confirmed, freshRelationship.Status);
        }
    }

    // This is the specific scenario named in the Task 1 audit brief: if
    // AvailabilitySlot.Book() did not persist IsConsumed, the Domain-level
    // double-booking guard (AvailabilitySlot.Book throwing when already
    // consumed) would be inert on every request after the first, and only
    // the database's own unique index on Sessions.AvailabilitySlotId would
    // still be preventing a double-booked slot.
    [Fact]
    public async Task BookSession_persists_AvailabilitySlot_IsConsumed_and_the_domain_guard_rejects_a_second_booking_of_the_same_slot()
    {
        var (_, _, _, slotId, _) = await BookSessionAsync();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshSlot = await dbContext.AvailabilitySlots.AsNoTracking()
                .FirstAsync(s => s.Id == AvailabilitySlotId.From(slotId));
            Assert.True(freshSlot.IsConsumed);
        }

        var (secondStudentId, secondStudentToken, _) = await RegisterAndLoginStudentAsync();
        var secondBookingResponse = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = slotId,
            StudentId = secondStudentId,
            ParentGuardianId = (Guid?)null,
        }, secondStudentToken);

        var secondBookingBody = await ReadBodyAsync(secondBookingResponse);
        Assert.True(secondBookingBody.GetProperty("isFailure").GetBoolean());
        // BookSessionCommand.InvalidState is the Domain guard (AvailabilitySlot.Book
        // throwing InvalidOperationException) — BookSessionCommand.SlotAlreadyBooked
        // would mean the guard was inert and only the DB unique index caught it.
        Assert.Equal(
            "BookSessionCommand.InvalidState",
            secondBookingBody.GetProperty("error").GetProperty("code").GetString());

        using var countScope = _factory.Services.CreateScope();
        var countDbContext = countScope.ServiceProvider.GetRequiredService<TutorFlowDbContext>();
        var count = await countDbContext.Sessions.AsNoTracking()
            .CountAsync(s => s.AvailabilitySlotId == AvailabilitySlotId.From(slotId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CancelSession_persists_Cancelled_status_when_reread_from_a_fresh_scope()
    {
        var (sessionId, studentToken, _, _, _) = await BookSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/cancel", body: null, studentToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshSession = await dbContext.Sessions.AsNoTracking().FirstAsync(s => s.Id == SessionId.From(sessionId));
            Assert.Equal(SessionStatus.Cancelled, freshSession.Status);
        }
    }

    // Phase 4.7 Task 3: proves the three-aggregate transaction (Session,
    // old AvailabilitySlot, new AvailabilitySlot) commits atomically — all
    // three re-read from a brand-new DbContext scope, never the one the
    // request pipeline used, per CLAUDE.md's rule that SaveChangesAsync
    // being called is not evidence anything was actually saved.
    [Fact]
    public async Task RescheduleSession_persists_all_three_aggregates_atomically_when_reread_from_a_fresh_scope()
    {
        var (sessionId, studentToken, tutorToken, oldSlotId, tutorId) = await BookSessionAsync();
        var newSlotResponse = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = DateTime.UtcNow.AddDays(5),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0,
        }, tutorToken);
        var newSlotBody = await ReadBodyAsync(newSlotResponse);
        var newSlotId = newSlotBody.GetProperty("value").GetProperty("availabilitySlotId").GetGuid();

        var response = await PostWithAuthAsync(
            $"/sessions/{sessionId}/reschedule", new { NewAvailabilitySlotId = newSlotId }, studentToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshSession = await dbContext.Sessions.AsNoTracking().FirstAsync(s => s.Id == SessionId.From(sessionId));
            Assert.Equal(AvailabilitySlotId.From(newSlotId), freshSession.AvailabilitySlotId);

            var freshOldSlot = await dbContext.AvailabilitySlots.AsNoTracking()
                .FirstAsync(s => s.Id == AvailabilitySlotId.From(oldSlotId));
            Assert.False(freshOldSlot.IsConsumed);

            var freshNewSlot = await dbContext.AvailabilitySlots.AsNoTracking()
                .FirstAsync(s => s.Id == AvailabilitySlotId.From(newSlotId));
            Assert.True(freshNewSlot.IsConsumed);
            Assert.Equal(freshNewSlot.StartTimeUtc, freshSession.ScheduledTimeUtc, TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public async Task CompleteSession_persists_Completed_status_when_reread_from_a_fresh_scope()
    {
        var (sessionId, _, tutorToken, _, _) = await BookSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/complete", body: null, tutorToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshSession = await dbContext.Sessions.AsNoTracking().FirstAsync(s => s.Id == SessionId.From(sessionId));
            Assert.Equal(SessionStatus.Completed, freshSession.Status);
        }
    }

    [Fact]
    public async Task MarkSessionNoShow_persists_NoShow_status_when_reread_from_a_fresh_scope()
    {
        var (sessionId, _, tutorToken, _, _) = await BookSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/no-show", body: null, tutorToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshSession = await dbContext.Sessions.AsNoTracking().FirstAsync(s => s.Id == SessionId.From(sessionId));
            Assert.Equal(SessionStatus.NoShow, freshSession.Status);
        }
    }
}
