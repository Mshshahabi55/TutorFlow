using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TutorFlow.Modules.Learning;

public static class LearningModule
{
    public static IServiceCollection AddLearningModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapLearningModule(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
