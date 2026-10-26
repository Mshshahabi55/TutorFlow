namespace TutorFlow.Modules.Payments.Tests;

/// <summary>
/// Every test class that builds a real <c>WebApplicationFactory&lt;Program&gt;</c> must share this
/// collection — same pattern and same reason as
/// <see cref="TutorFlow.Modules.Tutors.Tests.TutorsHostCollection"/>: xUnit runs separate
/// collections in parallel by default, and concurrently starting Wolverine hosts race on
/// Program.cs's static Serilog bootstrap logger. A44 #105 additionally depends on this: adding
/// <c>ProcessStripeWebhookTopologyFixture</c> (a second, genuinely-separate-container fixture)
/// also races <see cref="WolverineTestConnectionStrings.Configure"/>'s process-wide environment
/// variables against <c>PaymentsApiFixture</c>'s own — observed directly as
/// <c>PaymentsEndpointsTests</c> failures with "Failed to connect" to the wrong container's
/// already-recycled port when both fixtures initialize concurrently.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PaymentsHostCollection
{
    public const string Name = "Payments WebApplicationFactory<Program> host";
}
