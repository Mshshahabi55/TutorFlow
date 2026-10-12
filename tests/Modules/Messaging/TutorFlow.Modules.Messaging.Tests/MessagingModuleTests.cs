using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Messaging;

namespace TutorFlow.Modules.Messaging.Tests;

public class MessagingModuleTests
{
    [Fact]
    public void AddMessagingModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();

        services.AddMessagingModule();
    }
}
