using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TutorFlow.Modules.Search.Persistence;
using Wolverine;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TutorFlow.Modules.Availability;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Bookings;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Modules.Identity;
using TutorFlow.Modules.Identity.Infrastructure;
using TutorFlow.Modules.Identity.Persistence;
using TutorFlow.Modules.Learning;
using TutorFlow.Modules.Lessons;
using TutorFlow.Modules.Lessons.Persistence;
using TutorFlow.Modules.Messaging;
using TutorFlow.Modules.Notifications;
using TutorFlow.Modules.Notifications.Persistence;
using TutorFlow.Modules.Payments;
using TutorFlow.Modules.Payments.Persistence;
using TutorFlow.Modules.Reviews;
using TutorFlow.Modules.Search;
using TutorFlow.Modules.Students;
using TutorFlow.Modules.Subjects;
using TutorFlow.Modules.Subscriptions;
using TutorFlow.Modules.Tutors;
using TutorFlow.Modules.Tutors.Persistence;

var builder = WebApplication.CreateBuilder(args);

// First real cross-module messaging introduction (resolves OPEN_QUESTIONS.md A34) — the
// Tutors -> Search TutorPublished pipeline (ADR-0021). Local in-process transport only: no
// durable outbox persistence is configured yet, so a crash between Tutors' SaveChangesAsync
// and the publish is possible (messages are not crash-safe across a process restart). A
// Postgres-backed transactional outbox (WolverineFx.EntityFrameworkCore) is the documented
// long-term approach (CLAUDE.md, ADR-0015 "Messaging infrastructure") — flagged as a
// production-hardening follow-up, not silently glossed over.
// Dynamic (Roslyn-based) runtime compilation of handler code, via the referenced
// WolverineFx.RuntimeCompilation package — simplest path to get the first pipeline running.
// Pre-generating static code (`dotnet run -- codegen write` + TypeLoadMode.Static) and
// dropping the Roslyn dependency is the better production choice; flagged as a follow-up
// alongside the no-durable-outbox note above, not silently accepted as final.
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(SearchDbContext).Assembly);

    // Availability joins as a second TutorPublished subscriber, plus its own TutorUnpublished/
    // TutorAvailabilitySignalChanged consumers (ADR-0024) — same discovery/service-location
    // treatment Search already needed, for the same AddDbContext opaque-factory reason below.
    opts.Discovery.IncludeAssembly(typeof(AvailabilityDbContext).Assembly);

    // AddDbContext<T> registers DbContextOptions<T> via an opaque lambda factory, which
    // Wolverine 6's codegen refuses to resolve by default (InvalidServiceLocationException) —
    // allow-listing this one type keeps the strict policy for everything else rather than
    // flipping it globally (ServiceLocationPolicy.AllowedButWarn/AlwaysAllowed).
    opts.CodeGeneration.AlwaysUseServiceLocationFor<SearchDbContext>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<AvailabilityDbContext>();

    // Availability's new TutorPublished consumer transitively reaches Bookings'
    // IActiveBookingIntervalsQuery implementation (ADR-0014) through
    // GetTutorAvailabilityPreview — the same opaque-AddDbContext-factory issue applies to
    // BookingsDbContext the moment it's reachable from a Wolverine handler chain at all.
    opts.CodeGeneration.AlwaysUseServiceLocationFor<BookingsDbContext>();

    // Payments' webhook handler is the first real publisher Payments has ever had
    // (PaymentSucceeded/PaymentFailed) — Bookings' new consumers (PaymentSucceededConsumer/
    // PaymentFailedConsumer) are the first real Wolverine *handlers* Bookings has ever had, so
    // its assembly must now be registered for discovery too — previously it only implemented
    // query contracts, never a message handler (ADR-0015 "Messaging infrastructure", ADR-0027
    // §4). Same opaque-AddDbContext-factory treatment as every other DbContext reachable from a
    // Wolverine handler chain.
    opts.Discovery.IncludeAssembly(typeof(PaymentsDbContext).Assembly);
    opts.Discovery.IncludeAssembly(typeof(BookingsDbContext).Assembly);
    opts.CodeGeneration.AlwaysUseServiceLocationFor<PaymentsDbContext>();

    // Lessons' BookingConfirmedConsumer is its first real Wolverine handler ever (ADR-0016) —
    // Bookings is now also a publisher (ConfirmBooking.Handler), not just a consumer. Same
    // opaque-AddDbContext-factory treatment as every other DbContext reachable from a Wolverine
    // handler chain.
    opts.Discovery.IncludeAssembly(typeof(LessonsDbContext).Assembly);
    opts.CodeGeneration.AlwaysUseServiceLocationFor<LessonsDbContext>();

    // ADR-0029: Notifications' BookingHoldExpiredConsumer is its first real Wolverine handler
    // ever — Bookings' new BookingHoldExpirySweepService is now also a publisher
    // (BookingHoldExpired), not just a consumer of Payments' events. Same opaque-
    // AddDbContext-factory treatment as every other DbContext reachable from a Wolverine
    // handler chain. The consumer's handler also transitively reaches Identity's
    // IUserEmailQuery and Tutors' ITutorProfileSummaryQuery implementations (ADR-0014) — their
    // own DbContexts (IdentityDbContext, TutorsDbContext) become reachable from a Wolverine
    // handler chain for the first time here too, and need the identical treatment even though
    // neither module publishes/consumes any Wolverine message directly.
    opts.Discovery.IncludeAssembly(typeof(NotificationsDbContext).Assembly);
    opts.CodeGeneration.AlwaysUseServiceLocationFor<NotificationsDbContext>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IdentityDbContext>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<TutorsDbContext>();
});

builder.Services.AddOpenApi();

// String enum wire format (e.g. "Native" not "3") — applies Host-wide since every module
// shares one JSON configuration; added while wiring Tutors' SpokenLanguages/proficiency and
// Search's sort-option enums, both of which need a usable string contract for API consumers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// apps/web (Next.js) is a separate origin even in local dev (ADR-0009's monorepo topology
// is one repo, not one origin) — needed for the frontend's typed API client to reach this
// Host at all. No origin is allowed unless explicitly configured; nothing defaults to "*".
const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            // AllowCredentials() is required so the browser sends/receives the httpOnly
            // session cookie (ADR-0023) on cross-origin requests to this API; only safe
            // because origins are explicit above — the browser rejects AllowCredentials()
            // paired with a wildcard origin.
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Bound and validated once, by the Identity module's own JwtOptions (fails fast at startup via
// ValidateOnStart if Jwt:Issuer/Audience/Key are missing) — reused here instead of re-reading
// raw configuration a second time, which would bypass that startup validation.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
    {
        var options = jwtOptions.Value;
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            ClockSkew = TimeSpan.Zero
        };

        // Cookie-as-transport for the same stateless JWT (ADR-0023) — the frontend never
        // sees the token, so it can't attach an Authorization header itself. Falls back to
        // the header first so server-to-server/test clients using Bearer tokens still work.
        bearerOptions.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                {
                    context.Token = context.Request.Cookies[AuthCookie.Name];
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services
    .AddIdentityModule(builder.Configuration)
    .AddStudentsModule(builder.Configuration)
    .AddTutorsModule(builder.Configuration)
    .AddSubjectsModule(builder.Configuration)
    .AddSearchModule(builder.Configuration)
    .AddAvailabilityModule(builder.Configuration)
    .AddBookingsModule(builder.Configuration)
    .AddLessonsModule(builder.Configuration)
    .AddSubscriptionsModule()
    .AddPaymentsModule(builder.Configuration)
    .AddMessagingModule()
    .AddReviewsModule(builder.Configuration)
    .AddNotificationsModule(builder.Configuration)
    .AddLearningModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// Tutors' AddOutputCache(...) registers the "TutorPublicProfile" policy (ADR-0022); the core
// services/middleware are wired once, here, Host-wide — additional modules can register their
// own named policies via the same additive AddOutputCache(options => ...) pattern without a
// second UseOutputCache() call.
app.UseOutputCache();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapIdentityModule();
app.MapStudentsModule();
app.MapTutorsModule();
app.MapSubjectsModule();
app.MapSearchModule();
app.MapAvailabilityModule();
app.MapBookingsModule();
app.MapLessonsModule();
app.MapSubscriptionsModule();
app.MapPaymentsModule();
app.MapMessagingModule();
app.MapReviewsModule();
app.MapNotificationsModule();
app.MapLearningModule();

app.Run();

public partial class Program;
