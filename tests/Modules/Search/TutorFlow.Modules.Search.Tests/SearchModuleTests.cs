using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TutorFlow.Modules.Search.Tests;

public class SearchModuleTests
{
    [Fact]
    public void AddSearchModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SearchDb"] = "Host=localhost;Database=test",
            })
            .Build();

        services.AddSearchModule(config);
    }
}
