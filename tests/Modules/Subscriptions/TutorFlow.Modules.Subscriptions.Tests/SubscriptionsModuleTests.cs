using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Subscriptions;

namespace TutorFlow.Modules.Subscriptions.Tests;

public class SubscriptionsModuleTests
{
    [Fact]
    public void AddSubscriptionsModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();

        services.AddSubscriptionsModule();
    }
}
