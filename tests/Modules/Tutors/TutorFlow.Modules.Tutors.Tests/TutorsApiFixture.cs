using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using TutorFlow.Host;
using TutorFlow.Modules.Identity.Persistence;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Tutors.Tests;

public sealed class TutorsApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtIssuer = "TutorFlow.Tests";
    public const string JwtAudience = "TutorFlow.Tests";
    public const string JwtKey = "tutors-integration-test-signing-key-at-least-32-bytes-long";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    // Audit finding F6 regression test — Identity's own Postgres, only so IUserEmailQuery (which
    // SendCertificateRejectedEmail.Handler calls after a real RejectCertificate dispatch) has a
    // real ApplicationUser row to resolve, same reciprocal precedent as NotificationsApiFixture's
    // own Tutors/Identity containers for the equivalent abandoned-checkout pipeline.
    private readonly PostgreSqlContainer _identityPostgres = new PostgreSqlBuilder("postgres:18").Build();

    // A dedicated temp directory, not the real wwwroot — tests must never write into the
    // actual dev-tree static-file root (TutorPhotoStorage.ResolveRootPath honors this override).
    public string PhotoStorageRootPath { get; } =
        Path.Combine(Path.GetTempPath(), $"tutorflow-tests-photos-{Guid.NewGuid():N}");

    // Same reasoning, separate root — private certificate-document storage, never the same
    // directory as the (public, static-served) photo root.
    public string CertificateStorageRootPath { get; } =
        Path.Combine(Path.GetTempPath(), $"tutorflow-tests-certificates-{Guid.NewGuid():N}");

    private IDisposable? _wolverineConnectionStrings;

    // A44 #105: exposed so tests can query tutors.wolverine_outgoing_envelopes directly to prove
    // a rollback leaves no misleading outgoing event, without needing a second DbContext scope.
    public string TutorsConnectionString => _postgres.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TutorsDb"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:IdentityDb"] = _identityPostgres.GetConnectionString(),
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:Key"] = JwtKey,
                ["Jwt:ExpirationMinutes"] = "60",
                ["TutorPhotoStorage:RootPath"] = PhotoStorageRootPath,
                ["CertificateDocumentStorage:RootPath"] = CertificateStorageRootPath,
                // Fast interval — tests that drive the cleanup service call SweepOnceAsync
                // directly rather than waiting on the timer, but a short interval keeps the
                // service itself from ever being a meaningful delay if something does wait on it.
                ["CertificateDocumentCleanup:IntervalMinutes"] = "60"
            });
        });

        // Audit finding F6 regression test — re-registered as a singleton (the module itself
        // registers IEmailSender scoped) so a test can retrieve the exact instance the real
        // Wolverine dispatch sent through, same precedent as NotificationsApiFixture.
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, TestEmailSender>();
        });
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _identityPostgres.StartAsync());

        _wolverineConnectionStrings = WolverineTestConnectionStrings.Configure(_postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        await db.Database.MigrateAsync();
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await identityDb.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _identityPostgres.DisposeAsync().AsTask());
        if (Directory.Exists(PhotoStorageRootPath))
        {
            Directory.Delete(PhotoStorageRootPath, recursive: true);
        }

        if (Directory.Exists(CertificateStorageRootPath))
        {
            Directory.Delete(CertificateStorageRootPath, recursive: true);
        }

        await base.DisposeAsync();
        _wolverineConnectionStrings?.Dispose();
    }

    /// Mints a JWT locally with the given role(s), decoupled from the Identity module's
    /// login flow — Tutors must not depend on Identity to be tested, only on the shared
    /// JWT bearer contract (issuer/audience/key) that the Host wires centrally.
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
