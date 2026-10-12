using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Bookings;

namespace TutorFlow.Modules.Bookings.Tests;

public class BookingsModuleTests
{
    [Fact]
    public void AddBookingsModule_RegistersWithoutThrowing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddBookingsModule(config);
    }
}
