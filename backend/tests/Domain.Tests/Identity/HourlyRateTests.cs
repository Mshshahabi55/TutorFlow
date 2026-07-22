using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Tests.Identity;

// ADR-019: Rial has no minor unit (whole numbers only) and a bounded
// maximum (numeric(12,0)'s ceiling) — both enforced here, the single place
// per ADR-007's validation strategy, not duplicated in Application/Web.
public class HourlyRateTests
{
    [Fact]
    public void Of_with_a_positive_whole_amount_succeeds()
    {
        var rate = HourlyRate.Of(500_000m);

        Assert.Equal(500_000m, rate.Amount);
    }

    [Fact]
    public void Of_exposes_IRR_as_the_currency()
    {
        var rate = HourlyRate.Of(500_000m);

        Assert.Equal("IRR", rate.Currency);
        Assert.Equal("IRR", HourlyRate.CurrencyCode);
    }

    [Fact]
    public void Of_with_zero_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HourlyRate.Of(0m));
    }

    [Fact]
    public void Of_with_a_negative_amount_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HourlyRate.Of(-1m));
    }

    [Fact]
    public void Of_with_a_fractional_amount_throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => HourlyRate.Of(500_000.5m));
        Assert.Contains("whole number", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // Phase 4.5 (ADR-019 Addendum 1): a whole number that isn't a multiple
    // of 10 is a distinct, newer rejection reason from "fractional" above —
    // 45 is already an integer, but has no whole-Toman price a user could
    // have entered.
    [Fact]
    public void Of_with_a_whole_amount_not_divisible_by_ten_throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => HourlyRate.Of(45m));
        Assert.Contains("Toman", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Of_with_an_amount_divisible_by_ten_succeeds()
    {
        var rate = HourlyRate.Of(50m);

        Assert.Equal(50m, rate.Amount);
    }

    [Fact]
    public void Of_with_the_maximum_amount_succeeds()
    {
        var rate = HourlyRate.Of(HourlyRate.MaxAmount);

        Assert.Equal(HourlyRate.MaxAmount, rate.Amount);
    }

    [Fact]
    public void Of_with_an_amount_exceeding_the_maximum_throws()
    {
        // +10, not +1: MaxAmount is itself a multiple of 10 (Phase 4.5), so
        // +10 is the next value that violates *only* the maximum check,
        // isolating it from the divisibility check below.
        Assert.Throws<ArgumentOutOfRangeException>(() => HourlyRate.Of(HourlyRate.MaxAmount + 10m));
    }
}
