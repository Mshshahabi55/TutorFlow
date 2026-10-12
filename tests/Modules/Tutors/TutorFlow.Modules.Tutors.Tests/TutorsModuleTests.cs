using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Tutors;

namespace TutorFlow.Modules.Tutors.Tests;

public class TutorsModuleTests
{
    [Fact]
    public void AddTutorsModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TutorsDb"] = "Host=localhost;Database=test"
            })
            .Build();

        services.AddTutorsModule(config);
    }
}
