using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Meetings.ValueObjects;
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

    // --- ADR-024 (Accepted, 2026-07-28) — Tutor Onboarding Wizard ---

    [Fact]
    public async Task SetTutorPersonalInfo_persists_every_field_when_reread_from_a_fresh_scope()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync(
            $"/tutors/{tutorId}/personal-info",
            new
            {
                DisplayName = "Jane Doe",
                Headline = "Friendly Math Tutor",
                Biography = "I love teaching.",
                Country = "Iran",
                City = "Tehran",
                OtherLanguages = new[] { "French", "German" },
            },
            token);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.Equal("Jane Doe", freshTutor.DisplayName);
            Assert.Equal("Friendly Math Tutor", freshTutor.Headline);
            Assert.Equal("I love teaching.", freshTutor.Biography);
            Assert.Equal("Iran", freshTutor.Country);
            Assert.Equal("Tehran", freshTutor.City);
            Assert.Equal(new[] { "French", "German" }, freshTutor.OtherLanguages);
        }
    }

    [Fact]
    public async Task SetTutorTeachingInfo_persists_multiple_subjects_and_every_field_when_reread_from_a_fresh_scope()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync(
            $"/tutors/{tutorId}/teaching-info",
            new
            {
                TutorSubjects = new[]
                {
                    new { Subject = "Mathematics", Level = (string?)"Beginner" },
                    new { Subject = "Physics", Level = (string?)null },
                },
                YearsOfExperience = 5,
                Education = "BSc Mathematics",
                Certifications = "TEFL",
                TeachingMethodology = "Socratic method",
                LessonSpecialties = new[] { "Exam prep" },
            },
            token);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.Equal(2, freshTutor.TutorSubjects.Count);
            Assert.Contains(freshTutor.TutorSubjects, s => s.Subject == "Mathematics" && s.Level == "Beginner");
            Assert.Contains(freshTutor.TutorSubjects, s => s.Subject == "Physics" && s.Level == null);
            Assert.Equal(5, freshTutor.YearsOfExperience);
            Assert.Equal("BSc Mathematics", freshTutor.Education);
            Assert.Equal("TEFL", freshTutor.Certifications);
            Assert.Equal("Socratic method", freshTutor.TeachingMethodology);
            Assert.Equal(new[] { "Exam prep" }, freshTutor.LessonSpecialties);
        }
    }

    [Fact]
    public async Task SetTutorMedia_persists_every_url_when_reread_from_a_fresh_scope()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync(
            $"/tutors/{tutorId}/media",
            new
            {
                PhotoUrl = "https://example.com/photo.jpg",
                IntroVideoUrl = "https://example.com/intro.mp4",
                GalleryImageUrls = new[] { "https://example.com/1.jpg", "https://example.com/2.jpg" },
            },
            token);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.Equal("https://example.com/photo.jpg", freshTutor.PhotoUrl);
            Assert.Equal("https://example.com/intro.mp4", freshTutor.IntroVideoUrl);
            Assert.Equal(new[] { "https://example.com/1.jpg", "https://example.com/2.jpg" }, freshTutor.GalleryImageUrls);
        }
    }

    [Fact]
    public async Task SetTutorPricing_persists_hourly_rate_and_trial_lesson_pricing_when_reread_from_a_fresh_scope()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync(
            $"/tutors/{tutorId}/pricing",
            new { HourlyRateAmount = 500_000m, TrialLessonAvailable = true, TrialLessonPriceAmount = 100_000m },
            token);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.Equal(500_000m, freshTutor.HourlyRate?.Amount);
            Assert.True(freshTutor.TrialLessonAvailable);
            Assert.Equal(100_000m, freshTutor.TrialLessonPrice?.Amount);
        }
    }

    [Fact]
    public async Task SubmitTutorProfile_persists_ProfileStatus_Submitted_when_reread_from_a_fresh_scope()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();
        (await PatchWithAuthAsync($"/tutors/{tutorId}/subject", new { Subject = "Mathematics" }, token)).EnsureSuccessStatusCode();
        (await PatchWithAuthAsync($"/tutors/{tutorId}/hourly-rate", new { Amount = 500_000m }, token)).EnsureSuccessStatusCode();

        var response = await PostWithAuthAsync($"/tutors/{tutorId}/submit", body: null, token);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshTutor = await dbContext.Tutors.AsNoTracking().FirstAsync(t => t.Id == AccountId.From(tutorId));
            Assert.Equal(TutorProfileStatus.Submitted, freshTutor.ProfileStatus);
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

    // --- Communication context (RC5.1, docs/adr/ADR-022-communication-and-notifications-architecture.md) ---

    private async Task<Guid> StartConversationAsync(Guid targetAccountId, string callerToken)
    {
        var response = await PostWithAuthAsync("/conversations", new { TargetAccountId = targetAccountId }, callerToken);
        var body = await ReadBodyAsync(response);
        return body.GetProperty("value").GetProperty("conversationId").GetGuid();
    }

    [Fact]
    public async Task StartConversation_persists_the_Conversation_when_reread_from_a_fresh_scope()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, studentToken, _) = await RegisterAndLoginStudentAsync();

        var conversationId = await StartConversationAsync(tutorId, studentToken);

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshConversation = await dbContext.Conversations.AsNoTracking()
                .FirstAsync(c => c.Id == ConversationId.From(conversationId));
            Assert.NotNull(freshConversation);
        }
    }

    [Fact]
    public async Task SendMessage_persists_the_Message_and_the_Conversations_LastMessageAtUtc_when_reread_from_a_fresh_scope()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, studentToken, _) = await RegisterAndLoginStudentAsync();
        var conversationId = await StartConversationAsync(tutorId, studentToken);

        var response = await PostWithAuthAsync(
            $"/conversations/{conversationId}/messages", new { Body = "Are you available Tuesday?" }, studentToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshMessage = await dbContext.Messages.AsNoTracking()
                .FirstAsync(m => m.ConversationId == ConversationId.From(conversationId));
            Assert.Equal("Are you available Tuesday?", freshMessage.Body);

            var freshConversation = await dbContext.Conversations.AsNoTracking()
                .FirstAsync(c => c.Id == ConversationId.From(conversationId));
            Assert.NotNull(freshConversation.LastMessageAtUtc);
        }
    }

    [Fact]
    public async Task MarkConversationRead_persists_ReadAtUtc_on_every_unread_Message_addressed_to_the_caller()
    {
        var (tutorId, tutorToken) = await RegisterAndLoginTutorAsync();
        var (_, studentToken, _) = await RegisterAndLoginStudentAsync();
        var conversationId = await StartConversationAsync(tutorId, studentToken);
        (await PostWithAuthAsync(
            $"/conversations/{conversationId}/messages", new { Body = "Hello!" }, studentToken)).EnsureSuccessStatusCode();

        var response = await PostWithAuthAsync($"/conversations/{conversationId}/read", body: null, tutorToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshMessage = await dbContext.Messages.AsNoTracking()
                .FirstAsync(m => m.ConversationId == ConversationId.From(conversationId));
            Assert.NotNull(freshMessage.ReadAtUtc);
        }
    }

    [Fact]
    public async Task MarkNotificationRead_persists_ReadAtUtc_when_reread_from_a_fresh_scope()
    {
        var (tutorId, tutorToken) = await RegisterAndLoginTutorAsync();
        var (_, studentToken, _) = await RegisterAndLoginStudentAsync();
        var conversationId = await StartConversationAsync(tutorId, studentToken);
        (await PostWithAuthAsync(
            $"/conversations/{conversationId}/messages", new { Body = "Hi there" }, studentToken)).EnsureSuccessStatusCode();

        var notificationsResponse = await GetWithAuthAsync("/notifications/mine", tutorToken);
        var notificationId = (await ReadBodyAsync(notificationsResponse))
            .GetProperty("value")[0].GetProperty("notificationId").GetGuid();

        var response = await PostWithAuthAsync($"/notifications/{notificationId}/read", body: null, tutorToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshNotification = await dbContext.Notifications.AsNoTracking()
                .FirstAsync(n => n.Id == NotificationId.From(notificationId));
            Assert.NotNull(freshNotification.ReadAtUtc);
        }
    }

    [Fact]
    public async Task MarkAllNotificationsRead_persists_ReadAtUtc_on_every_Notification_addressed_to_the_caller()
    {
        var (_, studentToken, _, _, _) = await BookSessionAsync();

        var response = await PostWithAuthAsync("/notifications/mark-all-read", body: null, studentToken);
        response.EnsureSuccessStatusCode();

        var afterResponse = await GetWithAuthAsync("/notifications/mine", studentToken);
        var notifications = (await ReadBodyAsync(afterResponse)).GetProperty("value").EnumerateArray().ToList();

        Assert.NotEmpty(notifications);
        Assert.All(notifications, n => Assert.True(n.TryGetProperty("readAtUtc", out var readAt) && readAt.ValueKind != JsonValueKind.Null));
    }

    // Proves NotificationDomainEventHandler's reactive wiring end-to-end
    // through the real HTTP pipeline: booking a Session (Scheduling &
    // Booking context) raises SessionBooked, which — with zero direct
    // cross-context querying, per ADR-002 — produces a BookingConfirmed
    // Notification for the Student in the same transaction (ADR-022).
    [Fact]
    public async Task BookSession_reactively_persists_a_BookingConfirmed_Notification_for_the_Student()
    {
        var (sessionId, studentToken, _, _, _) = await BookSessionAsync();

        var response = await GetWithAuthAsync("/notifications/mine", studentToken);
        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var notifications = body.GetProperty("value").EnumerateArray().ToList();

        // NotificationType has no JsonStringEnumConverter registered (Program.cs
        // only adds the two UTC DateTime converters) — every enum in this API
        // already serializes as its underlying int (e.g. DeliveryMode), and
        // NotificationType.BookingConfirmed = 0 follows the same convention.
        Assert.Contains(notifications, n => n.GetProperty("type").GetInt32() == (int)NotificationType.BookingConfirmed);
    }

    // --- Meetings context (RC5.3, docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md) ---

    [Fact]
    public async Task StartLesson_persists_the_Meeting_when_reread_from_a_fresh_scope()
    {
        var (sessionId, _, tutorToken, _, _) = await BookSessionAsync();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/meeting", body: null, tutorToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshMeeting = await dbContext.Meetings.AsNoTracking()
                .FirstAsync(m => m.SessionId == SessionId.From(sessionId));
            Assert.Equal(MeetingStatus.Scheduled, freshMeeting.Status);
            Assert.StartsWith("https://mock-meeting.tutorflow.dev/join/", freshMeeting.JoinUrl);
        }
    }

    // Proves MeetingSyncDomainEventHandler's reactive, best-effort sync
    // (docs/adr/ADR-023-...): rescheduling a Session with an existing
    // Meeting updates that Meeting's own StartsAtUtc/EndsAtUtc in the same
    // transaction, without the reschedule request itself failing.
    [Fact]
    public async Task RescheduleSession_reactively_updates_the_existing_Meetings_own_times()
    {
        var (sessionId, studentToken, tutorToken, _, tutorId) = await BookSessionAsync();
        (await PostWithAuthAsync($"/sessions/{sessionId}/meeting", body: null, tutorToken)).EnsureSuccessStatusCode();

        var newSlotResponse = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = DateTime.UtcNow.AddDays(5),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0,
        }, tutorToken);
        var newSlotBody = await ReadBodyAsync(newSlotResponse);
        var newSlotId = newSlotBody.GetProperty("value").GetProperty("availabilitySlotId").GetGuid();

        var rescheduleResponse = await PostWithAuthAsync(
            $"/sessions/{sessionId}/reschedule", new { NewAvailabilitySlotId = newSlotId }, studentToken);
        rescheduleResponse.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshSession = await dbContext.Sessions.AsNoTracking().FirstAsync(s => s.Id == SessionId.From(sessionId));
            var freshMeeting = await dbContext.Meetings.AsNoTracking()
                .FirstAsync(m => m.SessionId == SessionId.From(sessionId));

            Assert.Equal(freshSession.ScheduledTimeUtc, freshMeeting.StartsAtUtc);
            Assert.Equal(freshSession.EndTimeUtc, freshMeeting.EndsAtUtc);
        }
    }

    // Proves the same reactive sync for cancellation — cancelling a Session
    // with an existing Meeting cancels that Meeting too, in the same
    // transaction, without the cancellation itself failing.
    [Fact]
    public async Task CancelSession_reactively_cancels_the_existing_Meeting()
    {
        var (sessionId, studentToken, tutorToken, _, _) = await BookSessionAsync();
        (await PostWithAuthAsync($"/sessions/{sessionId}/meeting", body: null, tutorToken)).EnsureSuccessStatusCode();

        var response = await PostWithAuthAsync($"/sessions/{sessionId}/cancel", body: null, studentToken);
        response.EnsureSuccessStatusCode();

        var dbContext = FreshDbContext(out var scope);
        using (scope)
        {
            var freshMeeting = await dbContext.Meetings.AsNoTracking()
                .FirstAsync(m => m.SessionId == SessionId.From(sessionId));
            Assert.Equal(MeetingStatus.Cancelled, freshMeeting.Status);
        }
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
}
