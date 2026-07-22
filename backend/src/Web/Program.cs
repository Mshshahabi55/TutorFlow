using Microsoft.EntityFrameworkCore;
using TutorFlow.Infrastructure;
using TutorFlow.Infrastructure.Persistence;
using TutorFlow.Web;
using TutorFlow.Web.DependencyInjection;
using TutorFlow.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// The "Testing" environment configures no provider here — a test host
// (e.g., TutorFlowWebApplicationFactory) supplies its own DbContext
// registration afterward, so Npgsql's services are never added in the
// first place rather than needing to be removed post hoc.
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddInfrastructure(_ => { });
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("TutorFlow")
        ?? throw new InvalidOperationException("Connection string 'TutorFlow' is not configured.");

    // Fail fast rather than silently running against placeholder credentials
    // outside Development — a wrong-but-present connection string is worse
    // than a missing one, since it fails later and less obviously.
    if (!builder.Environment.IsDevelopment() && connectionString.Contains("REPLACE_ME", StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "Connection string 'TutorFlow' still contains placeholder credentials ('REPLACE_ME'); configure a real connection string for this environment.");
    }

    builder.Services.AddInfrastructure(options => options.UseNpgsql(connectionString));
}

builder.Services.AddApplicationHandlers();

// Last-resort safety net for any exception a handler did not already
// translate into a Result (docs/adr/ADR-008-error-handling.md).
// AddProblemDetails() is required here even though GlobalExceptionHandler
// always writes its own ApiResponse-shaped body and returns true from
// TryHandleAsync (so IProblemDetailsService.TryWriteAsync is never actually
// invoked): UseExceptionHandler()'s middleware validates at startup that
// either an ExceptionHandlingPath/ExceptionHandler or a registered
// IProblemDetailsService exists, and throws immediately if neither does.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Constitution Success Criteria: an Admin/Staff member must be able to
// detect a problem without reading code or querying a database directly —
// this is the minimum, unopinionated first step toward that.
builder.Services.AddHealthChecks().AddDbContextCheck<TutorFlowDbContext>();

builder.Services.AddResponseCompression();

// OpenAPI document generation only — this describes the already-approved,
// already-public endpoint surface as-is. It does not decide contract
// versioning strategy (ARCHITECTURE.md §21 Item 7/ADR-010 OQ3-4, still open)
// or expose an interactive UI; "v1" here is the framework's own default
// document name, not a commitment to a multi-version scheme. Security-scheme
// annotations are intentionally absent — there is no authentication
// mechanism to describe while ADR-011 remains frozen.
var openApiEnabled = builder.Configuration.GetValue("OpenApi:Enabled", defaultValue: true);
if (openApiEnabled)
{
    builder.Services.AddOpenApi();
}

// Configuration-driven CORS: allowed origins come only from configuration.
// With none configured, the policy permits no cross-origin requests — a safe
// default, not a product decision — and that fact is logged at startup.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Configured", policy =>
    {
        if (corsAllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsAllowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

if (corsAllowedOrigins.Length == 0)
{
    app.Logger.LogInformation(
        "CORS: no allowed origins configured (Cors:AllowedOrigins); cross-origin browser requests will be rejected.");
}

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseResponseCompression();
app.UseCors("Configured");

// Authentication only — populates ICurrentUserProvider from a bearer token
// when one is presented and valid; makes no permission decision
// (docs/adr/ADR-017-authentication-mechanism-decision.md).
app.UseMiddleware<AuthenticationMiddleware>();

// Coarse-grained (Role -> Permission) authorization only, and only against
// endpoints that opt in via RequirePermission(...)
// (docs/adr/ADR-003-authentication-and-authorization.md; Launch
// Preparation, Priority 2, WP3). Must run after AuthenticationMiddleware
// (depends on ICurrentUserProvider already being populated) and after
// routing has resolved the endpoint's metadata — WebApplication inserts
// routing automatically near the start of the pipeline, so
// context.GetEndpoint() is already available here. No endpoint calls
// RequirePermission(...) yet (that is WP4's scope), so this middleware
// currently passes every request through unchanged, including /health
// (docs/adr/ADR-003 Addendum, Decision 5: Public, no special-casing needed
// here since it simply carries no RequiredPermissionMetadata).
app.UseMiddleware<AuthorizationMiddleware>();

app.MapHealthChecks("/health");

if (openApiEnabled)
{
    app.MapOpenApi();
}

app.MapApplicationEndpoints();

// Database:AutoMigrate (default: false) — no deployment platform is decided
// (ARCHITECTURE.md §21 Item 8), so migrations are not applied automatically
// unless explicitly opted into via configuration; this does not assume any
// hosting mechanism.
if (builder.Configuration.GetValue("Database:AutoMigrate", defaultValue: false))
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<TutorFlowDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Development-only seed data (docs/phases/PHASE-02-REPORT.md Task 6) — an
// explicit environment guard, not a configuration flag, since this must
// never be reachable in any environment where "Development" isn't already
// true for other reasons (e.g. the REPLACE_ME-placeholder bypass above).
if (app.Environment.IsDevelopment())
{
    await DevelopmentSeeder.SeedAsync(app.Services);
}

app.Run();

// Required so WebApplicationFactory<Program> (Phase 15: API end-to-end
// tests) can reference the entry point; top-level statements otherwise
// generate an internal Program class. Zero behavior change.
public partial class Program
{
}
