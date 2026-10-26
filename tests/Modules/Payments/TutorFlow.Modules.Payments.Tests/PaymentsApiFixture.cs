using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using TutorFlow.Host;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Modules.Lessons.Persistence;
using TutorFlow.Modules.Payments.Persistence;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Payments.Tests;

// Host-composed, four real schemas on one Postgres container (TutorPublishedPipelineFixture's
// established pattern) — Payments' own IBookingCheckoutEligibilityQuery dependency is real
// Bookings code (ADR-0019/ADR-0027), and seeding a payable booking needs Availability/Tutors'
// real prerequisite data, exactly like BookingsApiFixture's own tests.
public sealed class PaymentsApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public const string JwtIssuer = "TutorFlow.Tests";
    public const string JwtAudience = "TutorFlow.Tests";
    public const string JwtKey = "payments-integration-test-signing-key-at-least-32-bytes-long";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    private IDisposable? _wolverineConnectionStrings;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PaymentsDb"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:BookingsDb"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:AvailabilityDb"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:TutorsDb"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:LessonsDb"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:Key"] = JwtKey,
                ["Jwt:ExpirationMinutes"] = "60"
                // Payments:Stripe:SecretKey deliberately left unconfigured — selects
                // TestPaymentProviderClient (ADR-0027 §3), exactly like local dev.
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        _wolverineConnectionStrings = WolverineTestConnectionStrings.Configure(_postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<BookingsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<TutorsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<LessonsDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
        _wolverineConnectionStrings?.Dispose();
    }

    public static string CreateAccessToken(Guid userId, params string[] roles)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            Expires = DateTime.UtcNow.AddHours(1),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [ClaimTypes.NameIdentifier] = userId.ToString(),
                [ClaimTypes.Role] = roles
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
