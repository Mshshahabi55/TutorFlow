using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Infrastructure;
using TutorFlow.Modules.Bookings.Persistence;

namespace TutorFlow.Modules.Bookings.Tests;

/// <summary>
/// ADR-0029: the real, time-driven trigger for <c>BookingHoldExpired</c>. Scoped to Bookings'
/// own guarantee — which bookings transition to Expired, and which never do — exactly once per
/// sweep. The cross-module effect (Notifications actually receiving the event and attempting a
/// send) is proven separately in <c>TutorFlow.Host.Tests</c>
/// (<c>BookingHoldExpiryNotificationPipelineTests</c>), matching this codebase's established
/// division between module-level and full-pipeline tests.
/// </summary>
[Collection(BookingsHostCollection.Name)]
public class BookingHoldExpirySweepServiceTests(BookingsApiFixture fixture) : IClassFixture<BookingsApiFixture>
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    private async Task<Guid> SeedBookingAsync(BookingStatus status, DateTimeOffset holdExpiresAtUtc)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var start = Clock.GetUtcNow().AddDays(3);

        var result = Booking.CreateHeld(
            Guid.NewGuid(), Guid.NewGuid(), 25, start, holdExpiresAtUtc, new Money(2500, "EUR"), Clock, isTrial: true);
        Assert.True(result.IsSuccess);
        var booking = result.Value;

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (status != BookingStatus.Held)
        {
            await db.Bookings.Where(b => b.Id == booking.Id).ExecuteUpdateAsync(
                s => s.SetProperty(b => b.Status, status), TestContext.Current.CancellationToken);
        }

        return booking.Id;
    }

    private async Task<BookingStatus> GetStatusAsync(Guid bookingId)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        return await db.Bookings.AsNoTracking().Where(b => b.Id == bookingId).Select(b => b.Status).SingleAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SweepOnceAsync_StaleHeldBooking_TransitionsToExpired()
    {
        var bookingId = await SeedBookingAsync(BookingStatus.Held, Clock.GetUtcNow().AddMinutes(-5));
        var sweepService = fixture.Services.GetRequiredService<BookingHoldExpirySweepService>();

        await sweepService.SweepOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Expired, await GetStatusAsync(bookingId));
    }

    [Fact]
    public async Task SweepOnceAsync_StalePendingPaymentBooking_TransitionsToExpired()
    {
        var bookingId = await SeedBookingAsync(BookingStatus.PendingPayment, Clock.GetUtcNow().AddMinutes(-1));
        var sweepService = fixture.Services.GetRequiredService<BookingHoldExpirySweepService>();

        await sweepService.SweepOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Expired, await GetStatusAsync(bookingId));
    }

    [Fact]
    public async Task SweepOnceAsync_HeldBooking_NotYetExpired_IsLeftUntouched()
    {
        var bookingId = await SeedBookingAsync(BookingStatus.Held, Clock.GetUtcNow().AddMinutes(30));
        var sweepService = fixture.Services.GetRequiredService<BookingHoldExpirySweepService>();

        await sweepService.SweepOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Held, await GetStatusAsync(bookingId));
    }

    [Fact]
    public async Task SweepOnceAsync_ConfirmedBooking_IsNeverTouched_EvenWithAStaleHoldExpiresAtUtc()
    {
        // Confirmed bookings have HoldExpiresAtUtc cleared to null by ConfirmBooking in
        // production, but the sweep's Status filter alone must already be sufficient — this
        // test seeds the pathological case of a lingering past timestamp to prove that.
        var bookingId = await SeedBookingAsync(BookingStatus.Confirmed, Clock.GetUtcNow().AddMinutes(-10));
        var sweepService = fixture.Services.GetRequiredService<BookingHoldExpirySweepService>();

        await sweepService.SweepOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Confirmed, await GetStatusAsync(bookingId));
    }

    [Fact]
    public async Task SweepOnceAsync_CancelledBooking_IsNeverTouched()
    {
        var bookingId = await SeedBookingAsync(BookingStatus.Cancelled, Clock.GetUtcNow().AddMinutes(-10));
        var sweepService = fixture.Services.GetRequiredService<BookingHoldExpirySweepService>();

        await sweepService.SweepOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Cancelled, await GetStatusAsync(bookingId));
    }

    [Fact]
    public async Task SweepOnceAsync_CalledTwiceForTheSameStaleHold_SecondSweepFindsNothingLeftToDo()
    {
        var bookingId = await SeedBookingAsync(BookingStatus.Held, Clock.GetUtcNow().AddMinutes(-5));
        var sweepService = fixture.Services.GetRequiredService<BookingHoldExpirySweepService>();

        await sweepService.SweepOnceAsync(TestContext.Current.CancellationToken);
        // Must not throw, and must not somehow revert or duplicate anything on a second pass.
        await sweepService.SweepOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Expired, await GetStatusAsync(bookingId));
    }
}
