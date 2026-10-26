namespace TutorFlow.Host.Tests;

/// <summary>
/// Every test class that builds a real <c>WebApplicationFactory&lt;Program&gt;</c> must share this
/// collection. Program.cs's Serilog bootstrap logger (<c>Log.Logger = ...CreateBootstrapLogger()</c>)
/// is a static global reassigned and later frozen by <c>AddSerilog</c> each time the host starts;
/// xUnit runs separate collections in parallel by default, and two of those hosts starting
/// concurrently race on that static field ("The logger is already frozen."). Forcing these classes
/// into one collection makes them run sequentially relative to each other, which avoids the race
/// without changing the Serilog bootstrap itself. Same pattern as
/// <see cref="TutorFlow.Modules.Identity.Tests.IdentityHostCollection"/> in the Identity test
/// project (a separate process, so that fix doesn't cover this assembly).
/// </summary>
[CollectionDefinition(Name)]
public sealed class HostCollection
{
    public const string Name = "TutorFlow.Host WebApplicationFactory<Program> host";
}
