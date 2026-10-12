using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TutorFlow.Modules.Subscriptions;

public static class SubscriptionsModule
{
    public static IServiceCollection AddSubscriptionsModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapSubscriptionsModule(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
