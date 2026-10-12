using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Lessons;

namespace TutorFlow.Modules.Lessons.Tests;

public class LessonsModuleTests
{
    [Fact]
    public void AddLessonsModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddLessonsModule(config);
    }
}
