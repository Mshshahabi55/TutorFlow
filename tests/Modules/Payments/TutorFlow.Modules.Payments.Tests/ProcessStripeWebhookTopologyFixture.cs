using System.Security.Claims;
using System.Text;
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

// A44 #105: PaymentsApiFixture deliberately shares ONE Postgres container across all five
// Wolverine stores, which would mask the exact envelope-misrouting defect this fixture exists
// to rule out for ProcessStripeWebhookConsumer specifically (every schema physically coexists on
// one container, so a misrouted write to the wrong schema still lands on a container where that
// schema happens to exist). Two genuinely separate containers — Payments alone vs. everything
// else — reproduces the real production topology closely enough: if Wolverine's Eager-mode
// transactional middleware ever wrote PaymentSucceeded/PaymentFailed's outgoing envelope to any
// schema other than "payments" on the Payments container, that schema does not exist at all on
// the decoy container, and the write fails loudly (same "relation does not exist" signature
// already reproduced and documented for BookingHoldExpirySweepService's abandoned
// ExpireBookingHoldCommand attempt) instead of silently succeeding against a coincidentally
// present schema.
public sealed class ProcessStripeWebhookTopologyFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtIssuer = "TutorFlow.Tests";
    public const string JwtAudience = "TutorFlow.Tests";
    public const string JwtKey = "stripe-webhook-topology-test-signing-key-at-least-32-bytes";
    public const string WebhookSigningSecret = "whsec_topology_test_signing_secret";

    private readonly PostgreSqlContainer _paymentsPostgres = new PostgreSqlBuilder("postgres:18").Build();

    // Stands in for Bookings/Availability/Tutors/Lessons — deliberately one shared container
    // for all four, distinct from Payments' own container. The point is isolating Payments, not
    // re-proving the already-covered Bookings/Availability/Tutors/Lessons cross-module topology.
    private readonly PostgreSqlContainer _decoyPostgres = new PostgreSqlBuilder("postgres:18").Build();

    private IDisposable? _wolverineConnectionStrings;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PaymentsDb"] = _paymentsPostgres.GetConnectionString(),
                ["ConnectionStrings:BookingsDb"] = _decoyPostgres.GetConnectionString(),
                ["ConnectionStrings:AvailabilityDb"] = _decoyPostgres.GetConnectionString(),
                ["ConnectionStrings:TutorsDb"] = _decoyPostgres.GetConnectionString(),
                ["ConnectionStrings:LessonsDb"] = _decoyPostgres.GetConnectionString(),
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:Key"] = JwtKey,
                ["Jwt:ExpirationMinutes"] = "60",
                ["Payments:StripeWebhookSigningSecret"] = WebhookSigningSecret
                // Payments:Stripe:SecretKey deliberately left unconfigured — selects
                // TestPaymentProviderClient (ADR-0027 §3), exactly like local dev.
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_paymentsPostgres.StartAsync(), _decoyPostgres.StartAsync());

        _wolverineConnectionStrings = WolverineTestConnectionStrings.Configure(
            _decoyPostgres.GetConnectionString(),
            bookingsDb: _decoyPostgres.GetConnectionString(),
            availabilityDb: _decoyPostgres.GetConnectionString(),
            tutorsDb: _decoyPostgres.GetConnectionString(),
            paymentsDb: _paymentsPostgres.GetConnectionString());

        // Deliberately does NOT create a "payments" schema (or its envelope tables) anywhere on
        // the decoy container — that absence is exactly what turns a misroute into a loud,
        // observable failure instead of a silent success. See WolverineTestConnectionStrings.
        await WolverineTestConnectionStrings.ProvisionEnvelopeTablesAsync(
            _decoyPostgres.GetConnectionString(),
            bookingsDb: _decoyPostgres.GetConnectionString(),
            availabilityDb: _decoyPostgres.GetConnectionString(),
            tutorsDb: _decoyPostgres.GetConnectionString(),
            paymentsDb: _paymentsPostgres.GetConnectionString());

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<BookingsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<TutorsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<LessonsDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_paymentsPostgres.DisposeAsync().AsTask(), _decoyPostgres.DisposeAsync().AsTask());
        await base.DisposeAsync();
        _wolverineConnectionStrings?.Dispose();
    }

    public string PaymentsConnectionString => _paymentsPostgres.GetConnectionString();

    public string DecoyConnectionString => _decoyPostgres.GetConnectionString();

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
