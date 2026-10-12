using Microsoft.Extensions.Configuration;

namespace TutorFlow.Host.Tests;

/// <summary>
/// Pure unit tests for <see cref="RateLimitingConfigurationReader"/> — no host, no container, no
/// network: the eager-validation logic itself is a plain function of an <see cref="IConfiguration"/>
/// and a key, deliberately kept testable without spinning up a full <c>WebApplicationFactory</c>.
/// </summary>
public class RateLimitingConfigurationReaderTests
{
    private const string Key = "RateLimiting:Register:PermitLimit";

    private static IConfiguration BuildConfiguration(string? value) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [Key] = value })
            .Build();

    [Fact]
    public void ReadPositiveIntOrDefault_KeyAbsent_ReturnsDefault()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = RateLimitingConfigurationReader.ReadPositiveIntOrDefault(configuration, Key, 5);

        Assert.Equal(5, result);
    }

    [Fact]
    public void ReadPositiveIntOrDefault_ValidPositiveValue_ReturnsParsedValue()
    {
        var configuration = BuildConfiguration("12");

        var result = RateLimitingConfigurationReader.ReadPositiveIntOrDefault(configuration, Key, 5);

        Assert.Equal(12, result);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("not-a-number")]
    [InlineData("5.5")]
    [InlineData("")]
    public void ReadPositiveIntOrDefault_InvalidValue_ThrowsWithTheConfigurationKeyInTheMessage(string malformedValue)
    {
        var configuration = BuildConfiguration(malformedValue);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            RateLimitingConfigurationReader.ReadPositiveIntOrDefault(configuration, Key, 5));

        Assert.Contains(Key, ex.Message);
    }
}
