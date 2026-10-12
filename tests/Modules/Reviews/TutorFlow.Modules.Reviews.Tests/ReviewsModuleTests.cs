using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Reviews;

namespace TutorFlow.Modules.Reviews.Tests;

public class ReviewsModuleTests
{
    [Fact]
    public void AddReviewsModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddReviewsModule(config);
    }
}
