using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Availability;

namespace TutorFlow.Modules.Availability.Tests;

public class AvailabilityModuleTests
{
    [Fact]
    public void AddAvailabilityModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AvailabilityDb"] = "Host=localhost;Database=test"
            })
            .Build();

        services.AddAvailabilityModule(config);
    }
}
