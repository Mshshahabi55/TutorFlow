using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Learning;

namespace TutorFlow.Modules.Learning.Tests;

public class LearningModuleTests
{
    [Fact]
    public void AddLearningModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();

        services.AddLearningModule();
    }
}
