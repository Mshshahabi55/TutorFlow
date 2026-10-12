using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Notifications;

namespace TutorFlow.Modules.Notifications.Tests;

public class NotificationsModuleTests
{
    [Fact]
    public void AddNotificationsModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddNotificationsModule(config);
    }
}
