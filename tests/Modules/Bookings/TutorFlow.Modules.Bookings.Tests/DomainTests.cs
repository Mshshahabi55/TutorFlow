using TutorFlow.Modules.Bookings.Domain;

namespace TutorFlow.Modules.Bookings.Tests;

public class DomainTests
{
    private static readonly Money SamplePrice = new(5000, "USD");

    [Fact]
    public void CreateHeld_WithPositiveDuration_Succeeds()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var start = clock.GetUtcNow().AddDays(1);

        var result = Booking.CreateHeld(Guid.NewGuid(), Guid.NewGuid(), 25, start, start.AddMinutes(10), SamplePrice, clock, isTrial: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Held, result.Value.Status);
        Assert.Equal(start, result.Value.StartUtc);
        Assert.Equal(start.AddMinutes(25), result.Value.EndUtc);
        Assert.Equal(SamplePrice.AmountMinorUnits, result.Value.PriceAmountMinorUnits);
        Assert.Equal(SamplePrice.Currency, result.Value.PriceCurrency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateHeld_WithNonPositiveDuration_Fails(int durationMinutes)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var start = clock.GetUtcNow().AddDays(1);

        var result = Booking.CreateHeld(Guid.NewGuid(), Guid.NewGuid(), durationMinutes, start, start.AddMinutes(10), SamplePrice, clock, isTrial: true);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void CreateHeld_LinkedViaReschedule_CarriesRescheduledFromBookingId()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var start = clock.GetUtcNow().AddDays(1);
        var originalId = Guid.NewGuid();

        var result = Booking.CreateHeld(
            Guid.NewGuid(), Guid.NewGuid(), 25, start, start.AddMinutes(10), SamplePrice, clock, isTrial: true, rescheduledFromBookingId: originalId);

        Assert.True(result.IsSuccess);
        Assert.Equal(originalId, result.Value.RescheduledFromBookingId);
        Assert.Null(result.Value.RescheduledToBookingId);
    }

    [Fact]
    public void PriceSnapshot_IsImmutableAfterCreation()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var start = clock.GetUtcNow().AddDays(1);
        var booking = Booking.CreateHeld(Guid.NewGuid(), Guid.NewGuid(), 25, start, start.AddMinutes(10), SamplePrice, clock, isTrial: true).Value;

        // A tutor's current price changing after the fact (a different Money instance) must
        // never be reflected on an already-created Booking — it stores a snapshot, not a
        // live reference (BOOKINGS_SPEC.md §1.3).
        var laterPrice = new Money(9999, "EUR");

        Assert.Equal(SamplePrice, booking.Price);
        Assert.NotEqual(laterPrice, booking.Price);
    }

    [Theory]
    [InlineData(BookingStatus.Held, true)]
    [InlineData(BookingStatus.PendingPayment, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.Rescheduled, false)]
    [InlineData(BookingStatus.Expired, false)]
    [InlineData(BookingStatus.Completed, false)]
    [InlineData(BookingStatus.NoShow, false)]
    [InlineData(BookingStatus.Cancelled, false)]
    [InlineData(BookingStatus.Refunded, false)]
    public void ActiveStatuses_MatchesCancellableAndExclusionConstraintScope(BookingStatus status, bool expectedActive)
    {
        Assert.Equal(expectedActive, Booking.ActiveStatuses.Contains(status));
    }

    [Theory]
    [InlineData(BookingStatus.Rescheduled, true)]
    [InlineData(BookingStatus.Expired, true)]
    [InlineData(BookingStatus.Completed, true)]
    [InlineData(BookingStatus.NoShow, true)]
    [InlineData(BookingStatus.Cancelled, true)]
    [InlineData(BookingStatus.Refunded, true)]
    [InlineData(BookingStatus.Held, false)]
    [InlineData(BookingStatus.PendingPayment, false)]
    [InlineData(BookingStatus.Confirmed, false)]
    public void TerminalStatuses_MatchesBookingsSpecAuthoritativeTable(BookingStatus status, bool expectedTerminal)
    {
        Assert.Equal(expectedTerminal, Booking.TerminalStatuses.Contains(status));
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
