using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Students;

namespace TutorFlow.Modules.Students.Tests;

public class StudentsModuleTests
{
    [Fact]
    public void AddStudentsModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:StudentsDb"] = "Host=localhost;Database=test"
            })
            .Build();

        services.AddStudentsModule(config);
    }
}
