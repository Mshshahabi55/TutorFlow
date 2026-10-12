using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TutorFlow.Modules.Messaging;

public static class MessagingModule
{
    public static IServiceCollection AddMessagingModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapMessagingModule(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
