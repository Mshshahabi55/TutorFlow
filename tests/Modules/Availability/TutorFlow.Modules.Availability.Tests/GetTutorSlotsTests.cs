using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;
using GetTutorSlotsResponse = TutorFlow.Modules.Availability.Features.GetTutorSlots.GetTutorSlots.Response;

namespace TutorFlow.Modules.Availability.Tests;

/// <summary>
/// ADR-0025: GetTutorSlots evolved to be duration- and range-aware for trial slot selection.
/// Had zero endpoint-level test coverage before this phase.
/// </summary>
public sealed class GetTutorSlotsTests(AvailabilityApiFixture fixture) : IClassFixture<AvailabilityApiFixture>
{
    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AvailabilityApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    /// Seeds a fully-bookable tutor directly via the DB — BookingSettings + a rule covering
    /// every day of the week (wide enough that the test is independent of which real-world day
    /// it runs on) + TutorBookabilityRecords.IsBookable=true (which, in production, only the
    /// TutorPublished Wolverine consumer sets — there's no real Tutors flow in this fixture).
    private async Task<Guid> SeedBookableTutorAsync(int defaultSlotDurationMinutes = 60, int minNoticeHours = 0, int maxBookingWindowDays = 14)
    {
        var tutorUserId = Guid.NewGuid();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();

        var settings = BookingSettings.Create(tutorUserId, "UTC", minNoticeHours, maxBookingWindowDays, defaultSlotDurationMinutes, TimeProvider.System);
        db.BookingSettings.Add(settings.Value);

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            db.WeeklyAvailabilityRules.Add(WeeklyAvailabilityRule.Create(tutorUserId, day, new TimeOnly(9, 0), new TimeOnly(17, 0)).Value);
        }

        db.TutorBookabilityRecords.Add(TutorBookability.Create(tutorUserId, isBookable: true, TimeProvider.System));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return tutorUserId;
    }

    [Fact]
    public async Task GetTutorSlots_WithoutBookingSettings_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.GetAsync($"/api/availability/tutors/{Guid.NewGuid()}/slots", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTutorSlots_WhenNotBookable_ReturnsBadRequest()
    {
        var tutorUserId = Guid.NewGuid();
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
            db.BookingSettings.Add(BookingSettings.Create(tutorUserId, "UTC", 0, 14, 60, TimeProvider.System).Value);
            // No TutorBookabilityRecords row at all — same as a tutor who was never published.
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.GetAsync($"/api/availability/tutors/{tutorUserId}/slots", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTutorSlots_Unauthenticated_ReturnsUnauthorized()
    {
        var tutorUserId = await SeedBookableTutorAsync();
        var client = fixture.CreateClient();

        var response = await client.GetAsync($"/api/availability/tutors/{tutorUserId}/slots", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTutorSlots_WithoutDurationParam_UsesTheTutorsOwnDefaultDuration()
    {
        var tutorUserId = await SeedBookableTutorAsync(defaultSlotDurationMinutes: 60);
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.GetAsync($"/api/availability/tutors/{tutorUserId}/slots", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetTutorSlotsResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(60, body!.DurationMinutes);
        Assert.NotEmpty(body.Slots);
        Assert.All(body.Slots, s => Assert.Equal(TimeSpan.FromMinutes(60), s.EndUtc - s.StartUtc));
    }

    [Fact]
    public async Task GetTutorSlots_With25MinuteDuration_Returns25MinuteSlots_MoreOfThemThan50Minute()
    {
        // An 8-hour daily rule (9:00-17:00) fits sixteen 25-minute slots but only eight 50-minute
        // slots — this is "duration affects availability," not a client-side visual filter: each
        // call recomputes against the real backend with a different durationMinutes.
        var tutorUserId = await SeedBookableTutorAsync(maxBookingWindowDays: 1);
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response25 = await client.GetAsync($"/api/availability/tutors/{tutorUserId}/slots?durationMinutes=25", TestContext.Current.CancellationToken);
        var body25 = await response25.Content.ReadFromJsonAsync<GetTutorSlotsResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        var response50 = await client.GetAsync($"/api/availability/tutors/{tutorUserId}/slots?durationMinutes=50", TestContext.Current.CancellationToken);
        var body50 = await response50.Content.ReadFromJsonAsync<GetTutorSlotsResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.Equal(25, body25!.DurationMinutes);
        Assert.Equal(50, body50!.DurationMinutes);
        Assert.All(body25.Slots, s => Assert.Equal(TimeSpan.FromMinutes(25), s.EndUtc - s.StartUtc));
        Assert.All(body50.Slots, s => Assert.Equal(TimeSpan.FromMinutes(50), s.EndUtc - s.StartUtc));
        Assert.True(body25.Slots.Count > body50.Slots.Count);
    }

    [Fact]
    public async Task GetTutorSlots_DurationBelowStructuralLowerBound_ReturnsValidationProblem()
    {
        var tutorUserId = await SeedBookableTutorAsync();
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.GetAsync($"/api/availability/tutors/{tutorUserId}/slots?durationMinutes=5", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTutorSlots_ToUtcBeforeFromUtc_ReturnsValidationProblem()
    {
        var tutorUserId = await SeedBookableTutorAsync();
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var from = DateTimeOffset.UtcNow.AddDays(3);
        var to = DateTimeOffset.UtcNow.AddDays(1);

        var response = await client.GetAsync(
            $"/api/availability/tutors/{tutorUserId}/slots?fromUtc={Uri.EscapeDataString(from.ToString("O"))}&toUtc={Uri.EscapeDataString(to.ToString("O"))}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTutorSlots_WithoutExplicitRange_DefaultsToNextSevenDays()
    {
        // 30-day window configured, but no fromUtc/toUtc passed — the response must not include
        // slots from week three, proving the default range is one week, not the tutor's entire
        // booking window (ADR-0025's response-size-scoping decision).
        var tutorUserId = await SeedBookableTutorAsync(maxBookingWindowDays: 30);
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.GetAsync($"/api/availability/tutors/{tutorUserId}/slots", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetTutorSlotsResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.NotEmpty(body!.Slots);
        Assert.All(body.Slots, s => Assert.True(s.StartUtc <= DateTimeOffset.UtcNow.AddDays(7).AddMinutes(1)));
        Assert.Equal(30, body.MaxBookingWindowDays);
    }

    [Fact]
    public async Task GetTutorSlots_WithExplicitRange_ScopesResultsToThatWindow()
    {
        var tutorUserId = await SeedBookableTutorAsync(maxBookingWindowDays: 30);
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var from = DateTimeOffset.UtcNow.AddDays(10);
        var to = DateTimeOffset.UtcNow.AddDays(12);

        var response = await client.GetAsync(
            $"/api/availability/tutors/{tutorUserId}/slots?fromUtc={Uri.EscapeDataString(from.ToString("O"))}&toUtc={Uri.EscapeDataString(to.ToString("O"))}",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetTutorSlotsResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotEmpty(body!.Slots);
        Assert.All(body.Slots, s => Assert.InRange(s.StartUtc, from, to));
    }

    [Fact]
    public async Task GetTutorSlots_ExcludesAnActiveBookingInterval()
    {
        var tutorUserId = await SeedBookableTutorAsync(defaultSlotDurationMinutes: 60, maxBookingWindowDays: 1);
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        // First, confirm 09:00 UTC tomorrow would otherwise be open.
        var beforeResponse = await client.GetAsync($"/api/availability/tutors/{tutorUserId}/slots", TestContext.Current.CancellationToken);
        var before = await beforeResponse.Content.ReadFromJsonAsync<GetTutorSlotsResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotEmpty(before!.Slots);
        var targetSlot = before.Slots[0];

        using (var scope = fixture.Services.CreateScope())
        {
            var bookingsDb = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var held = Booking.CreateHeld(
                studentUserId: Guid.NewGuid(),
                tutorUserId: tutorUserId,
                durationMinutes: 60,
                startUtc: targetSlot.StartUtc,
                holdExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(10),
                price: new Money(2500, "EUR"),
                clock: TimeProvider.System,
                isTrial: true);
            bookingsDb.Bookings.Add(held.Value);
            await bookingsDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var afterResponse = await client.GetAsync($"/api/availability/tutors/{tutorUserId}/slots", TestContext.Current.CancellationToken);
        var after = await afterResponse.Content.ReadFromJsonAsync<GetTutorSlotsResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(after!.Slots, s => s.StartUtc == targetSlot.StartUtc);
    }

    [Fact]
    public async Task GetTutorSlots_ResponseIncludesTimeZoneAndDuration()
    {
        var tutorUserId = await SeedBookableTutorAsync(defaultSlotDurationMinutes: 50);
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.GetAsync($"/api/availability/tutors/{tutorUserId}/slots", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<GetTutorSlotsResponse>(AvailabilityApiFixture.JsonOptions, TestContext.Current.CancellationToken);

        Assert.NotNull(body);
        Assert.Equal("UTC", body!.TimeZoneId);
        Assert.Equal(50, body.DurationMinutes);
    }
}
