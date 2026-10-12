using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Subjects;

namespace TutorFlow.Modules.Subjects.Tests;

public class SubjectsModuleTests
{
    [Fact]
    public void AddSubjectsModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SubjectsDb"] = "Host=localhost;Database=test",
            })
            .Build();

        services.AddSubjectsModule(config);
    }
}
