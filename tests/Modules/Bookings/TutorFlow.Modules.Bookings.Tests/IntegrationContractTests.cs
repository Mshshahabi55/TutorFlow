using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Shared;
using CreateTrialHoldResponse = TutorFlow.Modules.Bookings.Features.CreateTrialHold.CreateTrialHold.Response;
using GetTutorSlotsResponse = TutorFlow.Modules.Availability.Features.GetTutorSlots.GetTutorSlots.Response;

namespace TutorFlow.Modules.Bookings.Tests;

// Proves the cross-module contracts (ADR-0014, ADR-0014 correction) actually wire up through
// the real Host composition root — not mocked substitutes — and that the fail-closed default
// (ADR-0014 correction) holds.
public class IntegrationContractTests(BookingsApiFixture fixture) : IClassFixture<BookingsApiFixture>
{
    [Fact]
    public async Task CompositionRoot_ResolvesAllThreeCrossModuleQueryContracts()
    {
        using var scope = fixture.Services.CreateScope();

        var availabilityQuery = scope.ServiceProvider.GetRequiredService<ITutorAvailabilityQuery>();
        var pricingQuery = scope.ServiceProvider.GetRequiredService<ITutorPricingQuery>();
        var activeIntervalsQuery = scope.ServiceProvider.GetRequiredService<IActiveBookingIntervalsQuery>();

        Assert.NotNull(availabilityQuery);
        Assert.NotNull(pricingQuery);
        Assert.NotNull(activeIntervalsQuery);

        // Each resolves to its OWNING module's assembly, not a stub/mock registered by mistake
        // in the wrong module or left unregistered — catches an accidental module-registration
        // regression at test time rather than at first real request.
        Assert.Equal("TutorFlow.Modules.Availability", availabilityQuery.GetType().Assembly.GetName().Name);
        Assert.Equal("TutorFlow.Modules.Tutors", pricingQuery.GetType().Assembly.GetName().Name);
        Assert.Equal("TutorFlow.Modules.Bookings", activeIntervalsQuery.GetType().Assembly.GetName().Name);
    }

    [Fact]
    public async Task TutorAvailabilityQuery_WithNoBookabilityRecord_FailsClosed()
    {
        using var scope = fixture.Services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<ITutorAvailabilityQuery>();
        var tutorId = Guid.NewGuid();
        var start = new DateTimeOffset(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5).ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);

        var result = await query.ValidateSlotAsync(tutorId, start, start.AddMinutes(25), TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Equal("tutor not bookable", result.Reason);
    }

    [Fact]
    public async Task TutorAvailabilityQuery_WithBookableTutor_ReturnsValidForAnArbitraryDuration()
    {
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        using var scope = fixture.Services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<ITutorAvailabilityQuery>();
        var start = new DateTimeOffset(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5).ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);

        // 50 minutes — not the tutor's configured 25-minute DefaultSlotDurationMinutes, proving
        // ADR-0014's generalized-duration requirement is actually implemented, not just
        // enumerable at the default duration.
        var result = await query.ValidateSlotAsync(tutorId, start, start.AddMinutes(50), TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task TutorPricingQuery_WithHourlyRateConfigured_ReturnsProratedPrice()
    {
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, hourlyRateMinorUnits: 6000, ct: TestContext.Current.CancellationToken);
        using var scope = fixture.Services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<ITutorPricingQuery>();

        var price = await query.GetCurrentPriceAsync(tutorId, 25, TestContext.Current.CancellationToken);

        Assert.NotNull(price);
        Assert.Equal(2500, price!.AmountMinorUnits);
        Assert.Equal("USD", price.Currency);
    }

    [Fact]
    public async Task TutorPricingQuery_WithNoTutorApplication_ReturnsNull()
    {
        using var scope = fixture.Services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<ITutorPricingQuery>();

        var price = await query.GetCurrentPriceAsync(Guid.NewGuid(), 25, TestContext.Current.CancellationToken);

        Assert.Null(price);
    }

    [Fact]
    public async Task ActiveBookingIntervalsQuery_ReflectsActiveHold_AndClearsAfterCancellation()
    {
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BookingsApiFixture.CreateAccessToken(Guid.NewGuid(), "Student"));
        var start = new DateTimeOffset(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(6).ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);

        var holdResponse = await client.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = start },
            TestContext.Current.CancellationToken);
        var booking = (await holdResponse.Content.ReadFromJsonAsync<CreateTrialHoldResponse>(BookingsApiFixture.JsonOptions, TestContext.Current.CancellationToken))!;

        using var scope = fixture.Services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IActiveBookingIntervalsQuery>();

        var whileHeld = await query.GetActiveIntervalsAsync(tutorId, start.AddDays(-1), start.AddDays(1), TestContext.Current.CancellationToken);
        Assert.Contains(whileHeld, i => i.StartUtc == start && i.EndUtc == start.AddMinutes(25));

        await client.PostAsJsonAsync($"/api/bookings/{booking.Id}/cancel", new { reason = (string?)null }, TestContext.Current.CancellationToken);

        var afterCancel = await query.GetActiveIntervalsAsync(tutorId, start.AddDays(-1), start.AddDays(1), TestContext.Current.CancellationToken);
        Assert.DoesNotContain(afterCancel, i => i.StartUtc == start);
    }

    [Fact]
    public async Task GetTutorSlots_ExcludesActiveBookingHold()
    {
        // Proves BOOKINGS_SPEC.md §5 prerequisite #3 end-to-end: Availability's public
        // browse-slots endpoint must not show a slot Bookings already holds.
        var tutorId = Guid.NewGuid();
        await TestTutorSetup.SeedBookableTutorAsync(fixture, tutorId, ct: TestContext.Current.CancellationToken);
        var studentClient = fixture.CreateClient();
        studentClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BookingsApiFixture.CreateAccessToken(Guid.NewGuid(), "Student"));
        // Must align exactly with the tutor's computed slot grid (stepping by the seeded
        // 25-minute DefaultSlotDurationMinutes from local midnight) — otherwise the assertion
        // below would trivially pass even if exclusion were broken, since the slot would never
        // have been enumerated as a candidate in the first place. 10:00 UTC = 600 minutes
        // past midnight = exactly 24 * 25.
        var start = new DateTimeOffset(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7).ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);

        await studentClient.PostAsJsonAsync(
            "/api/bookings/trial-holds",
            new { tutorUserId = tutorId, durationMinutes = 25, startUtc = start },
            TestContext.Current.CancellationToken);

        var anotherStudentClient = fixture.CreateClient();
        anotherStudentClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BookingsApiFixture.CreateAccessToken(Guid.NewGuid(), "Student"));

        var slotsResponse = await anotherStudentClient.GetFromJsonAsync<GetTutorSlotsResponse>(
            $"/api/availability/tutors/{tutorId}/slots", TestContext.Current.CancellationToken);

        Assert.NotNull(slotsResponse);
        Assert.DoesNotContain(slotsResponse!.Slots, s => s.StartUtc == start);
    }
}
