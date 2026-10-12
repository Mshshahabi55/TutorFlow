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
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Bookings.Infrastructure;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Modules.Lessons.Persistence;
using TutorFlow.Modules.Payments.Persistence;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Bookings.Tests;

// Three Postgres containers — one per module with a schema Bookings' first implementation
// slice actually exercises (Bookings itself, plus the two prerequisite modules — Availability
// for ITutorAvailabilityQuery/the browse-slots endpoint, Tutors for ITutorPricingQuery) — all
// through the real Host composition root, exactly like AvailabilityApiFixture/the established
// convention, so these tests prove real cross-module DI wiring, not a mocked substitute.
public sealed class BookingsApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Program.cs registers a Host-wide JsonStringEnumConverter — HttpClient's ReadFromJsonAsync
    // has no knowledge of that server-side DI configuration and needs matching options passed
    // explicitly to deserialize a string-enum wire value (e.g. BookingStatus) at all.
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public const string JwtIssuer = "TutorFlow.Tests";
    public const string JwtAudience = "TutorFlow.Tests";
    public const string JwtKey = "bookings-integration-test-signing-key-at-least-32-bytes-long";

    private readonly PostgreSqlContainer _bookingsPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _availabilityPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _tutorsPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _paymentsPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _lessonsPostgres = new PostgreSqlBuilder("postgres:18").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BookingsDb"] = _bookingsPostgres.GetConnectionString(),
                ["ConnectionStrings:AvailabilityDb"] = _availabilityPostgres.GetConnectionString(),
                ["ConnectionStrings:TutorsDb"] = _tutorsPostgres.GetConnectionString(),
                ["ConnectionStrings:PaymentsDb"] = _paymentsPostgres.GetConnectionString(),
                ["ConnectionStrings:LessonsDb"] = _lessonsPostgres.GetConnectionString(),
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:Key"] = JwtKey,
                ["Jwt:ExpirationMinutes"] = "60"
            });
        });

        // Bookings' real checkout path makes a genuine HTTP call to Payments (ADR-0019/
        // ADR-0027) — in production a real loopback socket call, in this in-memory test host
        // there is no real socket at all. Routing the named "Payments" client's primary handler
        // to this same TestServer (deferred until actually invoked, by which point the server
        // is already built) exercises the real Payments module's real endpoint/DB, not a mock.
        builder.ConfigureServices(services =>
            services.AddHttpClient(PaymentsApiClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler()));
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _bookingsPostgres.StartAsync(),
            _availabilityPostgres.StartAsync(),
            _tutorsPostgres.StartAsync(),
            _paymentsPostgres.StartAsync(),
            _lessonsPostgres.StartAsync());

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<BookingsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<TutorsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<LessonsDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _bookingsPostgres.DisposeAsync().AsTask(),
            _availabilityPostgres.DisposeAsync().AsTask(),
            _tutorsPostgres.DisposeAsync().AsTask(),
            _paymentsPostgres.DisposeAsync().AsTask(),
            _lessonsPostgres.DisposeAsync().AsTask());
        await base.DisposeAsync();
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
