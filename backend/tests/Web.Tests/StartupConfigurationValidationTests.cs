using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TutorFlow.Web.Tests;

// Proves the "fail fast on bad configuration" principle Program.cs already
// applies to a placeholder connection string now also covers RateLimiting
// and Meeting settings via ValidateOnStart — a misconfigured value must
// throw the moment the host starts, not the first time a request happens
// to touch the affected code path. Each test builds its own throwaway
// factory (never the shared IClassFixture one) since the point is that the
// host fails to start at all.
public class StartupConfigurationValidationTests
{
    [Fact]
    public void Host_fails_to_start_when_RateLimiting_General_PermitLimit_is_zero()
    {
        using var factory = new TutorFlowWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:General:PermitLimit"] = "0",
                })));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("PermitLimit", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Host_fails_to_start_when_RateLimiting_Auth_WindowSeconds_is_negative()
    {
        using var factory = new TutorFlowWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Auth:WindowSeconds"] = "-1",
                })));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("WindowSeconds", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Host_fails_to_start_when_Meeting_DefaultProvider_is_not_a_recognized_value()
    {
        using var factory = new TutorFlowWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Meeting:DefaultProvider"] = "NotARealProvider",
                })));

        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }

    // docs/adr/ADR-025-... Addendum — Booking Notice & Horizon (Accepted
    // 2026-07-29): same fail-fast discipline as RateLimiting/Meeting above.
    [Fact]
    public void Host_fails_to_start_when_SchedulingConstraints_MinimumBookingNoticeHours_is_zero()
    {
        using var factory = new TutorFlowWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SchedulingConstraints:MinimumBookingNoticeHours"] = "0",
                })));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("MinimumBookingNoticeHours", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Host_fails_to_start_when_SchedulingConstraints_MaximumBookingHorizonDays_is_negative()
    {
        using var factory = new TutorFlowWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SchedulingConstraints:MaximumBookingHorizonDays"] = "-1",
                })));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("MaximumBookingHorizonDays", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Host_fails_to_start_when_SchedulingConstraints_notice_meets_or_exceeds_horizon()
    {
        using var factory = new TutorFlowWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SchedulingConstraints:MinimumBookingNoticeHours"] = "2400",
                    ["SchedulingConstraints:MaximumBookingHorizonDays"] = "90",
                })));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("MinimumBookingNoticeHours", exception.ToString(), StringComparison.Ordinal);
    }
}
