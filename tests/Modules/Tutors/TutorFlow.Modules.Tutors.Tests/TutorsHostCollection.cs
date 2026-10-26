namespace TutorFlow.Modules.Tutors.Tests;

/// <summary>
/// Every test class that builds a real <c>WebApplicationFactory&lt;Program&gt;</c> must share this
/// collection. Program.cs's Serilog bootstrap logger (<c>Log.Logger = ...CreateBootstrapLogger()</c>)
/// is a static global reassigned and later frozen by <c>AddSerilog</c> each time the host starts;
/// xUnit runs separate collections in parallel by default, and two of those hosts starting
/// concurrently race on that static field ("The logger is already frozen."). Forcing these classes
/// into one collection makes them run sequentially relative to each other, which avoids the race
/// without changing the Serilog bootstrap itself. Same pattern as
/// <see cref="TutorFlow.Modules.Availability.Tests.AvailabilityHostCollection"/>/
/// <c>TutorFlow.Modules.Identity.Tests.IdentityHostCollection</c>/<c>TutorFlow.Host.Tests.HostCollection</c>
/// in their own separate test processes. Discovered here when adding <c>UploadProfilePhotoTests</c>
/// (a third <see cref="TutorsApiFixture"/>-based class) tipped an already-latent race — a full
/// Tutors suite run failed with "The logger is already frozen" across many tests, while any single
/// fixture-based class run in isolation passed cleanly; <c>TutorsEndpointsTests</c> and
/// <c>TutorsAdminEndpointsTests</c> were already racing before this change, just rarely enough (two
/// concurrent hosts, not three) not to have surfaced yet.
/// </summary>
[CollectionDefinition(Name)]
public sealed class TutorsHostCollection
{
    public const string Name = "Tutors WebApplicationFactory<Program> host";
}
