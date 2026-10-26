namespace TutorFlow.Modules.Bookings.Tests;

/// <summary>
/// Every test class that builds a real <c>WebApplicationFactory&lt;Program&gt;</c> must share this
/// collection. Program.cs's Serilog bootstrap logger (<c>Log.Logger = ...CreateBootstrapLogger()</c>)
/// is a static global reassigned and later frozen by <c>AddSerilog</c> each time the host starts;
/// xUnit runs separate collections in parallel by default, and two of those hosts starting
/// concurrently race on that static field ("The logger is already frozen."). Forcing these classes
/// into one collection makes them run sequentially relative to each other, which avoids the race
/// without changing the Serilog bootstrap itself. Same pattern as
/// <see cref="TutorFlow.Modules.Identity.Tests.IdentityHostCollection"/>/<c>TutorFlow.Host.Tests.HostCollection</c>/
/// <c>TutorFlow.Modules.Messaging.Tests.MessagingHostCollection</c>/<c>TutorFlow.Modules.Tutors.Tests.TutorsHostCollection</c>/
/// <c>TutorFlow.Modules.Availability.Tests.AvailabilityHostCollection</c> in their own separate test
/// processes. <c>BookingHoldExpirySweepServiceTests</c>, <c>BookingsEndpointsTests</c>,
/// <c>ConcurrencyTests</c>, and <c>IntegrationContractTests</c> each independently build
/// <see cref="BookingsApiFixture"/> without sharing a collection, so a full-suite run races on the
/// static logger — confirmed by running a single fixture-based class in isolation (passes cleanly)
/// versus the full suite (intermittent failures, all originating in
/// <c>BookingsApiFixture.InitializeAsync</c>), not by any logic change.
/// </summary>
[CollectionDefinition(Name)]
public sealed class BookingsHostCollection
{
    public const string Name = "Bookings WebApplicationFactory<Program> host";
}
