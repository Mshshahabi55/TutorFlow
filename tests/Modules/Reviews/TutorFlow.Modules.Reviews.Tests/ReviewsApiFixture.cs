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
using TutorFlow.Modules.Lessons.Persistence;
using TutorFlow.Modules.Reviews.Persistence;

namespace TutorFlow.Modules.Reviews.Tests;

// Real schemas on one real Host composition root — same established pattern as
// LessonsApiFixture/BookingsApiFixture. Reviews' eligibility check depends on Lessons via
// ICompletedLessonCountQuery (ADR-0014), so these tests seed real Lesson rows directly and
// exercise the real cross-module DI resolution, not a mocked substitute.
public sealed class ReviewsApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public const string JwtIssuer = "TutorFlow.Tests";
    public const string JwtAudience = "TutorFlow.Tests";
    public const string JwtKey = "reviews-integration-test-signing-key-at-least-32-bytes-long";

    private readonly PostgreSqlContainer _reviewsPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _lessonsPostgres = new PostgreSqlBuilder("postgres:18").Build();

    private IDisposable? _wolverineConnectionStrings;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ReviewsDb"] = _reviewsPostgres.GetConnectionString(),
                ["ConnectionStrings:LessonsDb"] = _lessonsPostgres.GetConnectionString(),
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:Key"] = JwtKey,
                ["Jwt:ExpirationMinutes"] = "60"
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_reviewsPostgres.StartAsync(), _lessonsPostgres.StartAsync());

        _wolverineConnectionStrings = WolverineTestConnectionStrings.Configure(_reviewsPostgres.GetConnectionString());

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ReviewsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<LessonsDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_reviewsPostgres.DisposeAsync().AsTask(), _lessonsPostgres.DisposeAsync().AsTask());
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
