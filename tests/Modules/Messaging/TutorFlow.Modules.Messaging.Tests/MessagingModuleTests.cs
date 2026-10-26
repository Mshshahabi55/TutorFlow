using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Messaging;

namespace TutorFlow.Modules.Messaging.Tests;

public class MessagingModuleTests
{
    [Fact]
    public void AddMessagingModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MessagingDb"] = "Host=localhost;Database=unused-for-this-smoke-test"
            })
            .Build();

        services.AddMessagingModule(config);
    }
}
