namespace TutorFlow.Modules.Lessons.Tests;

/// <summary>
/// Every test class that builds a real <c>WebApplicationFactory&lt;Program&gt;</c> must share this
/// collection. Two independent reasons, not just one: (1) Program.cs's Serilog bootstrap logger
/// is a static global reassigned and later frozen by <c>AddSerilog</c> each time the host starts
/// — concurrent starts race on it (same root cause already fixed this way in
/// <c>BookingsHostCollection</c>/<c>TutorsHostCollection</c>/etc., in their own separate test
/// processes). (2) A44: <see cref="LessonsApiFixture"/> points Wolverine's five message-store
/// connection strings at process-wide environment variables
/// (<c>TutorFlow.Host.WolverineTestConnectionStrings</c>), since <c>UseWolverine</c>'s eager
/// reads run before <c>WebApplicationFactory</c>'s own config overrides are ever applied — two
/// concurrently-starting instances in this project (<c>LessonCompletionTests</c>,
/// <c>LessonsEndpointsTests</c>, <c>LessonsPipelineTests</c> each independently build
/// <see cref="LessonsApiFixture"/>) would otherwise clobber each other's values mid-startup.
/// Forcing them into one collection makes them run sequentially relative to each other, closing
/// both races without changing the Serilog bootstrap or the env-var mechanism itself.
/// </summary>
[CollectionDefinition(Name)]
public sealed class LessonsHostCollection
{
    public const string Name = "Lessons WebApplicationFactory<Program> host";
}
