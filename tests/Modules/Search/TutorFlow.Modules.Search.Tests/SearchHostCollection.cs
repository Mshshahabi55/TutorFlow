namespace TutorFlow.Modules.Search.Tests;

/// <summary>
/// Every test class that builds a real <c>WebApplicationFactory&lt;Program&gt;</c> must share this
/// collection — same two reasons as <c>LessonsHostCollection</c>/<c>BookingsHostCollection</c>:
/// the Serilog bootstrap-logger race, and (A44) <see cref="SearchApiFixture"/> setting
/// Wolverine's five message-store connection strings via process-wide environment variables
/// (<c>TutorFlow.Host.WolverineTestConnectionStrings</c>), which two concurrently-starting
/// instances in this project (<c>SearchEndpointsTests</c>,
/// <c>TutorAvailabilitySignalChangedConsumerTests</c> each independently build
/// <see cref="SearchApiFixture"/>) would otherwise clobber mid-startup.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SearchHostCollection
{
    public const string Name = "Search WebApplicationFactory<Program> host";
}
