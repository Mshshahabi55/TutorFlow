using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Identity;

namespace TutorFlow.Modules.Identity.Tests;

public class IdentityModuleTests
{
    [Fact]
    public void AddIdentityModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentityDb"] = "Host=localhost;Database=test",
                ["Jwt:Issuer"] = "TutorFlow.Tests",
                ["Jwt:Audience"] = "TutorFlow.Tests",
                ["Jwt:Key"] = "unit-test-signing-key-at-least-32-bytes-long"
            })
            .Build();

        services.AddIdentityModule(config);
    }
}
