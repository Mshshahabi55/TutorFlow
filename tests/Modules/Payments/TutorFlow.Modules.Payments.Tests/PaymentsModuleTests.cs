using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Payments;

namespace TutorFlow.Modules.Payments.Tests;

public class PaymentsModuleTests
{
    [Fact]
    public void AddPaymentsModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddPaymentsModule(config);
    }
}
