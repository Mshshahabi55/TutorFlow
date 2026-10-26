namespace TutorFlow.Modules.Reviews.Tests;

/// <summary>
/// Every test class that builds a real <c>WebApplicationFactory&lt;Program&gt;</c> must share this
/// collection — same two reasons as <c>LessonsHostCollection</c>/<c>BookingsHostCollection</c>:
/// the Serilog bootstrap-logger race, and (A44) <see cref="ReviewsApiFixture"/> setting
/// Wolverine's five message-store connection strings via process-wide environment variables
/// (<c>TutorFlow.Host.WolverineTestConnectionStrings</c>), which two concurrently-starting
/// instances in this project (<c>ReviewEligibilityAndSubmissionTests</c>,
/// <c>SubcategoryRatingTests</c> each independently build <see cref="ReviewsApiFixture"/>) would
/// otherwise clobber mid-startup.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ReviewsHostCollection
{
    public const string Name = "Reviews WebApplicationFactory<Program> host";
}
