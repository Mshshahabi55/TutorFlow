using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Notifications;
using TutorFlow.Modules.Notifications.Infrastructure;

namespace TutorFlow.Modules.Notifications.Tests;

/// <summary>
/// Pure DI/options tests — no database, no fixture, no PeriodicTimer — proving misconfigured
/// reconciliation settings fail fast at options-validation time (Batch 50 finding: accessing
/// <see cref="IOptions{TOptions}.Value"/> is exactly what ValidateOnStart's own startup hosted
/// service does internally), rather than surfacing later inside
/// <see cref="PeriodicTimer"/>'s constructor or deep in the reconciliation loop. Calls the real
/// <c>AddNotificationsModule</c> registration directly (not a duplicated subset) so this proves
/// the actual wiring, not a parallel approximation of it — registering the module does not
/// require a reachable database, since <c>AddDbContext</c>'s configure delegate is not invoked
/// until a <c>DbContext</c> is actually resolved, which this test never does.
/// </summary>
public class NotificationsOptionsValidationTests
{
    private static Dictionary<string, string?> ValidSection() => new()
    {
        ["Notifications:ClaimLeaseMinutes"] = "10",
        ["Notifications:ClaimReconciliationIntervalSeconds"] = "60",
        ["Notifications:ClaimReconciliationBatchSize"] = "25"
    };

    private static ServiceProvider BuildProvider(Dictionary<string, string?> notificationsSection)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(notificationsSection).Build();
        var services = new ServiceCollection();
        services.AddNotificationsModule(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void ValidOptions_ResolveWithoutThrowing()
    {
        using var provider = BuildProvider(ValidSection());

        var options = provider.GetRequiredService<IOptions<NotificationsOptions>>().Value;

        Assert.Equal(10, options.ClaimLeaseMinutes);
        Assert.Equal(60, options.ClaimReconciliationIntervalSeconds);
        Assert.Equal(25, options.ClaimReconciliationBatchSize);
    }

    [Theory]
    [InlineData("Notifications:ClaimLeaseMinutes", "0")]
    [InlineData("Notifications:ClaimLeaseMinutes", "-5")]
    [InlineData("Notifications:ClaimReconciliationIntervalSeconds", "0")]
    [InlineData("Notifications:ClaimReconciliationIntervalSeconds", "-1")]
    [InlineData("Notifications:ClaimReconciliationBatchSize", "0")]
    [InlineData("Notifications:ClaimReconciliationBatchSize", "-3")]
    public void InvalidValue_FailsAtOptionsAccess_NotInsidePeriodicTimerOrReconciliationLoop(string key, string invalidValue)
    {
        var section = ValidSection();
        section[key] = invalidValue;
        using var provider = BuildProvider(section);

        var ex = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<NotificationsOptions>>().Value);

        Assert.Contains(key, ex.Message, StringComparison.Ordinal);
    }
}
