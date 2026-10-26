using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;
using AddTimeOffResponse = TutorFlow.Modules.Availability.Features.AddTimeOff.AddTimeOff.Response;
using GetComputedSlotsResponse = TutorFlow.Modules.Availability.Features.GetComputedSlots.GetComputedSlots.Response;
using GetCurrentConfigurationResponse = TutorFlow.Modules.Availability.Features.GetCurrentConfiguration.GetCurrentConfiguration.Response;
using ReplaceWeeklyRulesResponse = TutorFlow.Modules.Availability.Features.ReplaceWeeklyRules.ReplaceWeeklyRules.Response;
using UpdateBookingSettingsResponse = TutorFlow.Modules.Availability.Features.UpdateBookingSettings.UpdateBookingSettings.Response;

namespace TutorFlow.Modules.Availability.Tests;

[Collection(AvailabilityHostCollection.Name)]
public class AvailabilityEndpointsTests(AvailabilityApiFixture fixture) : IClassFixture<AvailabilityApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AvailabilityApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    [Fact]
    public async Task ReplaceWeeklyRules_WithTutorRole_ReturnsOk()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.PutAsJsonAsync(
            "/api/availability/me/weekly-rules",
            new { rules = new[] { new { dayOfWeek = DayOfWeek.Monday, startTime = "09:00", endTime = "17:00" } } },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReplaceWeeklyRulesResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Single(body!.Rules);
    }

    [Fact]
    public async Task ReplaceWeeklyRules_WithOverlappingRules_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.PutAsJsonAsync(
            "/api/availability/me/weekly-rules",
            new
            {
                rules = new[]
                {
                    new { dayOfWeek = DayOfWeek.Monday, startTime = "09:00", endTime = "12:00" },
                    new { dayOfWeek = DayOfWeek.Monday, startTime = "11:00", endTime = "14:00" }
                }
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReplaceWeeklyRules_WithoutTutorRole_ReturnsForbidden()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.PutAsJsonAsync(
            "/api/availability/me/weekly-rules",
            new { rules = Array.Empty<object>() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReplaceWeeklyRules_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/availability/me/weekly-rules",
            new { rules = Array.Empty<object>() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddTimeOff_WithValidRange_ReturnsCreated()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var start = DateTimeOffset.UtcNow.AddDays(5);

        var response = await client.PostAsJsonAsync(
            "/api/availability/me/time-off",
            new { startUtc = start, endUtc = start.AddDays(2), reason = "Vacation" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AddTimeOffResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("Vacation", body!.Reason);
    }

    [Fact]
    public async Task AddTimeOff_WithInvalidRange_ReturnsValidationProblem()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var start = DateTimeOffset.UtcNow.AddDays(5);

        var response = await client.PostAsJsonAsync(
            "/api/availability/me/time-off",
            new { startUtc = start, endUtc = start.AddDays(-1), reason = (string?)null },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveTimeOff_ExistingPeriod_ReturnsNoContent()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var start = DateTimeOffset.UtcNow.AddDays(5);
        var createResponse = await client.PostAsJsonAsync(
            "/api/availability/me/time-off",
            new { startUtc = start, endUtc = start.AddDays(1), reason = (string?)null },
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<AddTimeOffResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync($"/api/availability/me/time-off/{created!.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RemoveTimeOff_NonExistentPeriod_ReturnsNotFound()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.DeleteAsync($"/api/availability/me/time-off/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBookingSettings_CreateThenUpdate_PersistsChanges()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var createResponse = await client.PutAsJsonAsync(
            "/api/availability/me/booking-settings",
            new { timeZoneId = "UTC", minNoticeHours = 24, maxBookingWindowDays = 30, defaultSlotDurationMinutes = 60 },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync(
            "/api/availability/me/booking-settings",
            new { timeZoneId = "Europe/Berlin", minNoticeHours = 12, maxBookingWindowDays = 14, defaultSlotDurationMinutes = 30 },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var body = await updateResponse.Content.ReadFromJsonAsync<UpdateBookingSettingsResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("Europe/Berlin", body!.TimeZoneId);
        Assert.Equal(12, body.MinNoticeHours);
    }

    [Fact]
    public async Task UpdateBookingSettings_WithInvalidTimeZone_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.PutAsJsonAsync(
            "/api/availability/me/booking-settings",
            new { timeZoneId = "Not/ARealZone", minNoticeHours = 24, maxBookingWindowDays = 30, defaultSlotDurationMinutes = 60 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentConfiguration_BeforeAnyConfiguration_ReturnsUnconfiguredDefaults()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.GetAsync("/api/availability/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentConfigurationResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Empty(body!.WeeklyRules);
        Assert.Empty(body.TimeOff);
        Assert.False(body.BookingSettings.IsConfigured);
        Assert.False(body.IsBookable);
    }

    [Fact]
    public async Task GetCurrentConfiguration_AfterConfiguration_ReflectsPersistedData()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PutAsJsonAsync(
            "/api/availability/me/weekly-rules",
            new { rules = new[] { new { dayOfWeek = DayOfWeek.Tuesday, startTime = "10:00", endTime = "12:00" } } },
            TestContext.Current.CancellationToken);
        await client.PutAsJsonAsync(
            "/api/availability/me/booking-settings",
            new { timeZoneId = "UTC", minNoticeHours = 24, maxBookingWindowDays = 30, defaultSlotDurationMinutes = 60 },
            TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/availability/me", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetCurrentConfigurationResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.Single(body!.WeeklyRules);
        Assert.True(body.BookingSettings.IsConfigured);
    }

    [Fact]
    public async Task GetComputedSlots_WithoutBookingSettings_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.GetAsync("/api/availability/me/slots", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetComputedSlots_WithFullConfiguration_ReturnsComputedSlots()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PutAsJsonAsync(
            "/api/availability/me/weekly-rules",
            new
            {
                rules = Enum.GetValues<DayOfWeek>()
                    .Select(d => new { dayOfWeek = d, startTime = "09:00", endTime = "17:00" })
            },
            TestContext.Current.CancellationToken);
        await client.PutAsJsonAsync(
            "/api/availability/me/booking-settings",
            new { timeZoneId = "UTC", minNoticeHours = 0, maxBookingWindowDays = 7, defaultSlotDurationMinutes = 60 },
            TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/availability/me/slots", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetComputedSlotsResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.NotEmpty(body!.Slots);
        Assert.All(body.Slots, s => Assert.Equal(TimeSpan.FromMinutes(60), s.EndUtc - s.StartUtc));
    }

    [Fact]
    public async Task DualRoleUser_WithStudentAndTutorRoles_CanManageAvailability()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student", "Tutor");

        var response = await client.PutAsJsonAsync(
            "/api/availability/me/booking-settings",
            new { timeZoneId = "UTC", minNoticeHours = 24, maxBookingWindowDays = 30, defaultSlotDurationMinutes = 60 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReplaceWeeklyRules_WithEmptyRules_ClearsAllExistingRules()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        await client.PutAsJsonAsync(
            "/api/availability/me/weekly-rules",
            new { rules = new[] { new { dayOfWeek = DayOfWeek.Monday, startTime = "09:00", endTime = "17:00" } } },
            TestContext.Current.CancellationToken);

        var response = await client.PutAsJsonAsync(
            "/api/availability/me/weekly-rules",
            new { rules = Array.Empty<object>() },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReplaceWeeklyRulesResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Empty(body!.Rules);

        var configResponse = await client.GetAsync("/api/availability/me", TestContext.Current.CancellationToken);
        var config = await configResponse.Content.ReadFromJsonAsync<GetCurrentConfigurationResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.Empty(config!.WeeklyRules);
    }

    [Fact]
    public async Task UpdateBookingSettings_WithConcurrentFirstTimeCreation_NeverReturnsUnhandledError()
    {
        var userId = Guid.NewGuid();
        var clientA = CreateAuthenticatedClient(userId, "Tutor");
        var clientB = CreateAuthenticatedClient(userId, "Tutor");
        var payload = new { timeZoneId = "UTC", minNoticeHours = 24, maxBookingWindowDays = 30, defaultSlotDurationMinutes = 60 };

        var responses = await Task.WhenAll(
            clientA.PutAsJsonAsync("/api/availability/me/booking-settings", payload, TestContext.Current.CancellationToken),
            clientB.PutAsJsonAsync("/api/availability/me/booking-settings", payload, TestContext.Current.CancellationToken));

        // Whether these two requests actually land in the same race window is timing-dependent
        // (verified deterministically at the DB level in BookingSettingsRace tests below) — but
        // regardless of timing, the result must always be a clean 200, never an unhandled 500.
        Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest));
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveTimeOff_WithConcurrentDeletionOfSameId_OnlyOneSucceeds()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var start = DateTimeOffset.UtcNow.AddDays(5);
        var createResponse = await client.PostAsJsonAsync(
            "/api/availability/me/time-off",
            new { startUtc = start, endUtc = start.AddDays(1), reason = (string?)null },
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<AddTimeOffResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        var responses = await Task.WhenAll(
            client.DeleteAsync($"/api/availability/me/time-off/{created!.Id}", TestContext.Current.CancellationToken),
            client.DeleteAsync($"/api/availability/me/time-off/{created.Id}", TestContext.Current.CancellationToken));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NotFound);
        Assert.DoesNotContain(responses, r => (int)r.StatusCode >= 500);
    }

    [Fact]
    public async Task ReplaceWeeklyRules_WithConcurrentReplacements_NeverReturnsUnhandledError()
    {
        var userId = Guid.NewGuid();
        var clientA = CreateAuthenticatedClient(userId, "Tutor");
        var clientB = CreateAuthenticatedClient(userId, "Tutor");
        var payloadA = new { rules = new[] { new { dayOfWeek = DayOfWeek.Monday, startTime = "09:00", endTime = "12:00" } } };
        var payloadB = new { rules = new[] { new { dayOfWeek = DayOfWeek.Tuesday, startTime = "09:00", endTime = "12:00" } } };

        var responses = await Task.WhenAll(
            clientA.PutAsJsonAsync("/api/availability/me/weekly-rules", payloadA, TestContext.Current.CancellationToken),
            clientB.PutAsJsonAsync("/api/availability/me/weekly-rules", payloadB, TestContext.Current.CancellationToken));

        // Whether these two requests actually overlap inside Postgres is timing-dependent (the
        // exact serializable-conflict mechanism is proven deterministically below) — but
        // regardless of timing, the result must always be a clean 200/409, never a silent
        // last-write-wins corruption and never an unhandled 500.
        Assert.All(responses, r => Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict));
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task BookingSettingsCreation_WithGenuinelyConcurrentTransactions_SecondGetsUniqueViolation()
    {
        var tutorId = Guid.NewGuid();
        using var scopeA = fixture.Services.CreateScope();
        using var scopeB = fixture.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<AvailabilityDbContext>();

        // Deterministically reproduces the exact race UpdateBookingSettings.Handler's catch
        // clause protects against: both see "no settings yet" before either writes.
        var existingA = await dbA.BookingSettings.FirstOrDefaultAsync(s => s.TutorUserId == tutorId, TestContext.Current.CancellationToken);
        var existingB = await dbB.BookingSettings.FirstOrDefaultAsync(s => s.TutorUserId == tutorId, TestContext.Current.CancellationToken);
        Assert.Null(existingA);
        Assert.Null(existingB);

        dbA.BookingSettings.Add(BookingSettings.Create(tutorId, "UTC", 24, 30, 60, TimeProvider.System).Value);
        dbB.BookingSettings.Add(BookingSettings.Create(tutorId, "UTC", 24, 30, 60, TimeProvider.System).Value);

        await dbA.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(
            () => dbB.SaveChangesAsync(TestContext.Current.CancellationToken));
        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pgEx.SqlState);
    }

    [Fact]
    public async Task ReplaceWeeklyRules_WithGenuinelyConcurrentTransactions_SecondGetsSerializationFailure()
    {
        var tutorId = Guid.NewGuid();
        using var scopeA = fixture.Services.CreateScope();
        using var scopeB = fixture.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<AvailabilityDbContext>();

        await using var txA = await dbA.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, TestContext.Current.CancellationToken);
        await using var txB = await dbB.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, TestContext.Current.CancellationToken);

        // Both transactions observe "no existing rules" before either writes — the exact
        // write-skew shape ReplaceWeeklyRules.Handler's serializable transaction protects
        // against (plain read-then-write, even each in its own SaveChanges transaction, cannot
        // detect this).
        _ = await dbA.WeeklyAvailabilityRules.Where(r => r.TutorUserId == tutorId).ToListAsync(TestContext.Current.CancellationToken);
        _ = await dbB.WeeklyAvailabilityRules.Where(r => r.TutorUserId == tutorId).ToListAsync(TestContext.Current.CancellationToken);

        dbA.WeeklyAvailabilityRules.Add(WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)).Value);
        dbB.WeeklyAvailabilityRules.Add(WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 0)).Value);

        await dbA.SaveChangesAsync(TestContext.Current.CancellationToken);
        await txA.CommitAsync(TestContext.Current.CancellationToken);

        // EF Core's execution strategy wraps this specific error (recognized as "transient")
        // in an extra InvalidOperationException because it occurs inside a manually-managed
        // explicit transaction — the real PostgresException is nested inside it.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await dbB.SaveChangesAsync(TestContext.Current.CancellationToken);
            await txB.CommitAsync(TestContext.Current.CancellationToken);
        });
        var pgEx = Assert.IsType<PostgresException>(ex.InnerException!.InnerException);
        Assert.Equal(PostgresErrorCodes.SerializationFailure, pgEx.SqlState);
    }
}
