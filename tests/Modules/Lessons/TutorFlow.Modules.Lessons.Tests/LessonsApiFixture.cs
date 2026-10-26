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
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Modules.Lessons.Persistence;

namespace TutorFlow.Modules.Lessons.Tests;

// Two real schemas on one Postgres container, through the real Host composition root — the
// same established pattern as BookingsApiFixture/PaymentsApiFixture. Only Bookings + Lessons
// are needed here: these tests seed a Booking row directly (ConfirmBooking.Handler has no
// Availability/Tutors dependency of its own), then exercise the real BookingConfirmed ->
// Lessons pipeline exactly as it runs in production.
//
// Genuinely one container, not two — a prior version of this fixture started a second,
// separate container for Lessons, which this comment's own stated intent never actually
// matched. That went unnoticed until A44 added a real cross-module Wolverine dispatch test
// here: Wolverine's EfCoreEnvelopeTransaction marks an incoming LOCAL message as handled
// through the consuming handler's OWN DbContext connection (LessonsDbContext here), writing to
// the Host-owned Main store's "messaging" schema specifically — which only exists on whichever
// container WolverineDb resolves to. Two separate containers meant "messaging" lived on
// Bookings' container while Lessons' connection pointed at an entirely different one, so the
// write failed with "relation messaging.wolverine_incoming_envelopes does not exist" no matter
// when the table was created. One container for every schema this fixture touches is the fix.
public sealed class LessonsApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Program.cs registers a Host-wide JsonStringEnumConverter — HttpClient's ReadFromJsonAsync
    // has no knowledge of that server-side DI configuration and needs matching options passed
    // explicitly to deserialize a string-enum wire value (e.g. LessonStatus) at all.
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public const string JwtIssuer = "TutorFlow.Tests";
    public const string JwtAudience = "TutorFlow.Tests";
    public const string JwtKey = "lessons-integration-test-signing-key-at-least-32-bytes-long";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    private IDisposable? _wolverineConnectionStrings;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BookingsDb"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:LessonsDb"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = "TutorFlow.Tests",
                ["Jwt:Audience"] = "TutorFlow.Tests",
                ["Jwt:Key"] = "lessons-integration-test-signing-key-at-least-32-bytes-long",
                ["Jwt:ExpirationMinutes"] = "60"
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        _wolverineConnectionStrings = WolverineTestConnectionStrings.Configure(_postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<BookingsDbContext>().Database.MigrateAsync();
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
