using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using JasperFx;
using JasperFx.CodeGeneration;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Search.Persistence;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence.Durability;
using Wolverine.Postgresql;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TutorFlow.Host;
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
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Modules.Notifications.Persistence;
using TutorFlow.Modules.Payments;
using TutorFlow.Modules.Payments.Persistence;
using TutorFlow.Modules.Reviews;
using TutorFlow.Modules.Search;
using TutorFlow.Modules.Students;
using TutorFlow.Modules.Subjects;
using TutorFlow.Modules.Subscriptions;
using TutorFlow.Modules.Tutors;
using TutorFlow.Modules.Tutors.Infrastructure;
using TutorFlow.Modules.Tutors.Persistence;

// Stage 1 (Kit `serilog` skill): a bootstrap logger captures startup failures that happen
// before configuration/DI exist to build the full logger below — otherwise an exception
// thrown before that point would be lost with no log output at all.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TutorFlow.Host");

    var builder = WebApplication.CreateBuilder(args);

    // Stage 2: the full logger, configured from appsettings.json's "Serilog" section and
    // wired into the DI-registered ILogger<T> pipeline via ReadFrom.Services — replaces the
    // bootstrap logger above once configuration is available.
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // First real cross-module messaging introduction (resolves OPEN_QUESTIONS.md A34) — the
    // Tutors -> Search TutorPublished pipeline (ADR-0021). Local in-process transport only: no
    // durable outbox persistence is configured yet, so a crash between Tutors' SaveChangesAsync
    // and the publish is possible (messages are not crash-safe across a process restart). A
    // Postgres-backed transactional outbox (WolverineFx.EntityFrameworkCore) is the documented
    // long-term approach (CLAUDE.md, ADR-0015 "Messaging infrastructure") — flagged as a
    // production-hardening follow-up, not silently glossed over (ADR-0021 addendum, A44).
    builder.Host.UseWolverine(opts =>
    {
        // ADR-0021 addendum (F6 production reliability fix) — Dynamic TypeLoadMode compiles
        // each message type's handler chain lazily, via Roslyn, on its first dispatch. Two
        // different message types' first-ever compilation racing concurrently corrupts
        // JasperFx's internal frame-arranging state (a known upstream bug class —
        // JasperFx/wolverine#1734 — not specific to any one handler here: reproduced against
        // both TutorCertificateRejected and, independently, TutorPublished/
        // TutorAvailabilitySignalChanged), silently moving the affected envelope to Wolverine's
        // local error queue instead of invoking the consumer. Static mode removes runtime
        // codegen — and this entire race — by compiling every handler chain once, ahead of
        // time, via `dotnet run --project src/TutorFlow.Host -- codegen write` (output committed
        // under Internal/Generated/, verified for drift in CI — see backend.yml). Applied
        // unconditionally, in every environment including tests, so the code the test suite
        // exercises is the same code that ships to production — not gated behind
        // IsProduction(), which would leave the test suite (and local dev) still exposed to the
        // race this fix exists to close.
        opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;

        // F6 follow-on discovery: `codegen write`'s ahead-of-time pass generates code for every
        // discovered handler-shaped type in one go, which surfaced something Dynamic mode's
        // lazy, only-on-actual-dispatch compilation never did — Wolverine's default discovery
        // convention treats ANY public type whose name ends in "Handler" (one of four built-in
        // match rules, confirmed against this exact version's docs) as a candidate handler, with
        // its first method parameter read as "the message type" regardless of what that type
        // actually is. Every ordinary CQRS feature class in this codebase is named `Handler`
        // (e.g. `RejectCertificate.Handler`, `CancelBookingForPaymentFailure.Handler`) and was
        // being swept up as a false positive — one such class's first HandleAsync parameter is a
        // bare `Guid`, which is what produced the "Message Handler for System.Guid"
        // InvalidServiceLocationException that blocked `codegen write` entirely.
        //
        // Tried first: opts.Discovery.DisableConventionalDiscovery() + decorating all 10 real
        // consumers with [WolverineHandler]. Measurably wrong — confirmed by actually running
        // `codegen write` against it: it produced "Wolverine found no handlers" and an empty
        // GeneratedHandlerRegistry, so the attribute does not work independently of conventional
        // (scanning-based) discovery in this version; disabling the scan disables the
        // attribute-based match too. Excluding only the "Handler" suffix rule — while every real
        // consumer here already independently matches the still-active "Consumer" suffix rule —
        // is the version that actually works (verified: codegen write succeeds, every real
        // consumer still gets a generated file). The [WolverineHandler] attributes on all 10
        // consumer classes are kept anyway as the officially-documented "more deliberate
        // selection" marker (belt-and-suspenders, not load-bearing on their own here).
        opts.Discovery.CustomizeHandlerDiscovery(query => query.Excludes.WithNameSuffix("Handler"));

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

        // A44 #103: Tutors never needed its own assembly registered for discovery before — it
        // only ever published (PublishAsync doesn't need the publishing assembly scanned), never
        // hosted a Wolverine handler of its own. Now it does: PublishApplicationConsumer/
        // SuspendApplicationConsumer/RejectCertificateConsumer are local-command dispatch targets
        // (see PublishApplication.cs's own remarks) that close the crash window between each
        // feature's business write and its outgoing TutorPublished/TutorUnpublished/
        // TutorCertificateRejected event by running inside a real Wolverine chain instead of being
        // called as a plain DI class directly from the HTTP endpoint.
        opts.Discovery.IncludeAssembly(typeof(TutorsDbContext).Assembly);

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

        // F6 production reliability fix (Static codegen) surfaced this: NotificationsModule's
        // own IEmailSender registration (`services.AddScoped<IEmailSender>(sp => ...)`) is the
        // same kind of opaque lambda factory as the AddDbContext registrations above, and
        // `codegen write` enforces the ServiceLocationPolicy.NotAllowed default strictly, ahead
        // of time, across every discovered handler in one pass — surfacing this immediately,
        // where Dynamic mode's lazy, per-chain-on-first-dispatch compilation never happened to
        // hit it. Same narrow, single-type allow-listing precedent as every DbContext above, not
        // a policy relaxation.
        opts.CodeGeneration.AlwaysUseServiceLocationFor<IEmailSender>();

        // A44 durable outbox. One Main store (this app's own node/leader-election/control-queue
        // bookkeeping — infrastructure, not any module's business data, hence its own
        // Host-owned "messaging" schema/database rather than piggybacking on a business
        // module) plus one Ancillary store per publishing module, each pointed at that same
        // module's own connection string/schema so its business write and outgoing envelope
        // commit in the same database transaction (verified in the A44 spike: this requires
        // BOTH AddDbContextWithWolverineIntegration on the DbContext registration above/in each
        // module AND this explicit PersistMessagesWithPostgresql call — registering only one of
        // the two does not create that module's envelope tables).
        opts.PersistMessagesWithPostgresql(
            builder.Configuration.GetConnectionString("WolverineDb") ?? string.Empty,
            "messaging",
            MessageStoreRole.Main);
        // A44 #103 follow-up — store-resolution investigation (bounded fallback, see
        // docs/OPEN_QUESTIONS.md's A44 entry for the full writeup): Wolverine.Postgresql's
        // PostgresqlBackedPersistence.Enroll<T>() is the SUPPORTED, type-safe way to associate an
        // Ancillary store with its owning DbContext type (it registers an AncillaryMessageStore
        // keyed by that type into IWolverineRuntime.Stores, which AutoApplyTransactions consults
        // via HasAncillaryStoreFor(type) to insert ApplyAncillaryStoreFrame<T> — the frame whose
        // AncillaryMessageStoreApplication<T>.Apply(context) pins MessageContext.Storage by type,
        // not by registration order). It was tried here and confirmed to work for its own stated
        // purpose: every outgoing envelope was empirically order-independent afterward. It was
        // REVERTED anyway, because it has a confirmed, reproducible side effect on OTHER,
        // already-correct cross-module Lightweight consumers (PaymentSucceededConsumer and
        // PaymentFailedConsumer, both in Bookings): the generated chain still runs
        // EnlistInOutboxAsync/EnrollDbContextInTransaction BEFORE ApplyAncillaryStoreFrame<T>'s
        // own Apply(context) call (confirmed directly from the generated `codegen write` output,
        // not inferred) — so the very first delivery of a message type that Enroll<T>() newly
        // started tracking, through the SAME store-pinning machinery this fix needed, durably
        // persisted its incoming envelope using a stale/default store reference and 42P01'd
        // against the wrong schema, landing 2 dead letters before a later, independently-
        // recovered attempt succeeded. Existing replay-safety (ConfirmBooking's
        // PaymentReferenceId-gated AlreadyConfirmed path) absorbed this cleanly with no wrong
        // business outcome in every run observed, but dead-lettering on first delivery is a real,
        // unintended regression on paths this task was not scoped to touch — not shipped.
        // [Storage(typeof(X))] and an explicit [Transactional(typeof(X))] argument were also
        // tried on both consumers and did not change the outcome, confirming this is Wolverine
        // 6.45.0's own EF Core transaction-middleware codegen ordering, not an application-level
        // attribute gap — patching it would mean forking/wrapping Wolverine's own codegen, a
        // broader architecture change outside this task's bound. Falling back, as explicitly
        // authorized: the current registration order (Payments last) is the load-bearing
        // invariant ProcessStripeWebhook's own envelope targeting depends on — do not reorder
        // these four calls without re-running ProcessStripeWebhookTopologyTests (genuinely
        // separate per-store containers; it will fail loudly with a "relation ... does not exist"
        // error if Payments stops being last) and re-evaluating this decision.
        opts.PersistMessagesWithPostgresql(
            builder.Configuration.GetConnectionString("BookingsDb") ?? string.Empty,
            "bookings",
            MessageStoreRole.Ancillary);
        opts.PersistMessagesWithPostgresql(
            builder.Configuration.GetConnectionString("AvailabilityDb") ?? string.Empty,
            "availability",
            MessageStoreRole.Ancillary);
        opts.PersistMessagesWithPostgresql(
            builder.Configuration.GetConnectionString("TutorsDb") ?? string.Empty,
            "tutors",
            MessageStoreRole.Ancillary);
        opts.PersistMessagesWithPostgresql(
            builder.Configuration.GetConnectionString("PaymentsDb") ?? string.Empty,
            "payments",
            MessageStoreRole.Ancillary);

        // Wraps every handler whose first DbContext dependency participates in the outbox (any
        // of the four modules above) in that DbContext's own transaction, committing the
        // business write and the outgoing/scheduled envelope together (verified in the A44
        // spike, including for handlers that use ExecuteUpdateAsync rather than
        // SaveChangesAsync — both enlist in the same ambient transaction this opens).
        opts.UseEntityFrameworkCoreTransactions();
        opts.Policies.AutoApplyTransactions();

        // Required for durability on LOCAL (in-process) message delivery specifically — without
        // this, a cascaded message to another module's consumer in the same process is only
        // fire-and-forget in memory, even though its envelope was durably persisted; this policy
        // is what makes the persisted envelope the actual source of truth for delivery/retry
        // rather than a side record no one reads back from on recovery.
        opts.Policies.UseDurableLocalQueues();
    }, ExtensionDiscovery.ManualOnly);
    // ^ ManualOnly, not the default Automatic: Automatic makes Wolverine scan every assembly in
    // this app's deployment closure (via WolverineOptions.DiscoverAndApplyExtensions ->
    // ModuleAssemblyLoader.EnsureDeployedModuleAssembliesAreLoaded) for OTHER NuGet packages that
    // register themselves as Wolverine extensions — a feature this app never uses (every module
    // is wired explicitly via opts.Discovery.IncludeAssembly above, never auto-discovered).
    // Reaching RunJasperFxCommands(args) for the first time (replacing app.Run(), required for
    // Static TypeLoadMode's `codegen write` command to be reachable at all) exercises that scan
    // for the first time too, and it throws CultureNotFoundException under this project's
    // InvariantGlobalization=true — Assembly.GetName() on a satellite resource assembly inside a
    // culture-named output folder (e.g. bin/.../cs/Microsoft.Testing.Platform.resources.dll, from
    // the test framework, present in every test project's own output) needs to parse "cs" as a
    // CultureInfo, which InvariantGlobalization mode never supports. Confirmed version-specific:
    // JasperFx/Wolverine's own upstream main branch already special-cases exactly this scenario
    // ("production failures with localized dependencies") in a way our installed 2.79.1 does
    // not. ManualOnly skips the scan entirely rather than working around the bug — correct both
    // because we have zero real extensions to discover, and because masking a thrown exception
    // here would hide a genuine future case if an actual extension-registering package were ever
    // added without this comment being revisited.

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

    // JwtBearer stays the default scheme — Google/Facebook/the temporary external-login cookie
    // are additional, named schemes only ever invoked explicitly (TypedResults.Challenge /
    // HttpContext.AuthenticateAsync with an explicit scheme name in IdentityModule's external
    // login endpoints), never as a fallback for [Authorize]/RequireAuthorization(). Real
    // credentials/callback paths/claim mapping are configured separately, in
    // IdentityModule.AddIdentityModule, via the same AddOptions<T>(scheme).Configure(...)
    // two-step pattern already used for JwtBearerOptions below — that keeps this module's own
    // configuration-binding concern out of the Host.
    //
    // Google/Facebook are registered conditionally, only when real credentials are configured —
    // confirmed by direct testing (not an assumption): ASP.NET Core's AuthenticationMiddleware
    // resolves every registered IAuthenticationRequestHandler scheme on every single incoming
    // request (to let each check its own CallbackPath), and resolving an OAuth handler eagerly
    // validates its Options — OAuthOptions.Validate() throws ArgumentException for an empty
    // ClientId/ClientSecret. Registering either scheme with empty placeholder credentials
    // therefore 500s *every* request in the entire application, not just OAuth-specific routes —
    // this broke all 19 of the existing IdentityEndpointsTests (register/login/logout) when
    // first tried unconditionally. Registering only the schemes that actually have credentials
    // avoids the broken handler ever existing, matching how any app with an optional OAuth
    // provider is meant to be wired.
    var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
    var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    var facebookClientId = builder.Configuration["Authentication:Facebook:ClientId"];
    var facebookClientSecret = builder.Configuration["Authentication:Facebook:ClientSecret"];
    var isGoogleConfigured = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);
    var isFacebookConfigured = !string.IsNullOrWhiteSpace(facebookClientId) && !string.IsNullOrWhiteSpace(facebookClientSecret);

    var authenticationBuilder = builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer()
        .AddCookie(ExternalLoginConstants.TemporaryScheme);

    if (isGoogleConfigured)
    {
        authenticationBuilder.AddGoogle();
    }

    if (isFacebookConfigured)
    {
        authenticationBuilder.AddFacebook();
    }

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

    // Login/Register are the only two anonymous, auth-sensitive endpoints (ADR none yet —
    // narrowly scoped hardening pass). Partitioned by the direct TCP connection's remote
    // address, never a client-controlled header: no ForwardedHeadersMiddleware/reverse-proxy
    // config exists anywhere in this repo yet, so Connection.RemoteIpAddress is the genuine
    // source and can't be spoofed by an arbitrary request header. If a reverse proxy is
    // introduced later, UseForwardedHeaders with explicit KnownProxies/KnownNetworks must be
    // configured first, or this partition key would become trivially bypassable.
    // Complements, not replaces, ASP.NET Core Identity's existing per-account lockout
    // (MaxFailedAccessAttempts/DefaultLockoutTimeSpan above) — that guards one targeted account;
    // this guards against one source hammering many accounts or flooding registration.
    // Captured by the policy factories below and assigned once, eagerly, right after
    // builder.Build() (see the validation block just after that call) — not read fresh from
    // configuration inside each partition's own factory lambda. Declared here, before
    // AddRateLimiter, only because the lambdas below must close over them; the values
    // themselves are not trustworthy until after Build() (a test host's own configuration
    // overrides — see IdentityApiFixture/AuthRateLimitingApiFixture — are not guaranteed visible
    // to builder.Configuration until the builder has actually finished building).
    int registerPermitLimit = default, registerWindowSeconds = default, loginPermitLimit = default, loginWindowSeconds = default;

    // Messaging's abuse scenario is different from auth's: a single already-authenticated
    // account spamming many tutors, not an unauthenticated source hammering an endpoint — so
    // these two policies partition by the caller's authenticated UserId claim, not IP. This is
    // only possible because UseRateLimiter() runs after UseAuthentication() below (moved
    // deliberately for this reason) so HttpContext.User is already populated by the time these
    // factories run; the pre-auth auth-register/auth-login policies above are unaffected since
    // they partition by IP and never touch User at all.
    int messagingCreateConversationPermitLimit = default, messagingCreateConversationWindowSeconds = default,
        messagingSendMessagePermitLimit = default, messagingSendMessageWindowSeconds = default;

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, ct) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            }

            context.HttpContext.Response.ContentType = "application/problem+json";
            await context.HttpContext.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests.",
                    Detail = "Too many requests from this client. Please try again later."
                },
                ct);
        };

        options.AddPolicy("auth-register", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = registerPermitLimit,
                Window = TimeSpan.FromSeconds(registerWindowSeconds),
                QueueLimit = 0
            }));

        options.AddPolicy("auth-login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPermitLimit,
                Window = TimeSpan.FromSeconds(loginWindowSeconds),
                QueueLimit = 0
            }));

        // Partitioned by the authenticated caller's own NameIdentifier claim — never a
        // client-supplied user ID, so a request cannot spend a different user's quota by
        // claiming their ID in a header or body field. These endpoints already require
        // authentication (RequireAuthorization), so User.Identity is always populated by the
        // time this factory runs; "unknown" is an unreachable fallback, not a real code path.
        options.AddPolicy("messaging-create-conversation", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = messagingCreateConversationPermitLimit,
                Window = TimeSpan.FromSeconds(messagingCreateConversationWindowSeconds),
                QueueLimit = 0
            }));

        options.AddPolicy("messaging-send-message", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = messagingSendMessagePermitLimit,
                Window = TimeSpan.FromSeconds(messagingSendMessageWindowSeconds),
                QueueLimit = 0
            }));
    });

    // A44: registered only so `dotnet ef migrations`/`Database.MigrateAsync()` tooling has a
    // DbContext to target for the Main store's own envelope/node tables (see
    // WolverineMessagingDbContext's doc comment) — never injected into any handler, since it
    // has no business entities. The actual Wolverine Main-store wiring is the
    // PersistMessagesWithPostgresql call inside UseWolverine above, which is independent of
    // this DI registration.
    builder.Services.AddDbContext<WolverineMessagingDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("WolverineDb")));

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
        .AddMessagingModule(builder.Configuration)
        .AddReviewsModule(builder.Configuration)
        .AddNotificationsModule(builder.Configuration)
        .AddLearningModule();

    // Must exist before Build() — ASP.NET Core fixes WebRootFileProvider to a NullFileProvider
    // at host-build time if the directory doesn't exist yet, even if it's created afterward, so
    // UseStaticFiles() below would silently never serve anything (see TutorPhotoStorage's own
    // doc comment for the storage decision itself).
    Directory.CreateDirectory(TutorPhotoStorage.ResolveRootPath(
        builder.Environment, builder.Configuration["TutorPhotoStorage:RootPath"]));

    var app = builder.Build();

    // Read and validate once, here, right after Build() — app.Configuration is now guaranteed
    // final (a test host's own configuration overrides are only reliably visible from this
    // point on), and this still runs before any request can reach a rate-limiting policy
    // factory. A malformed, zero, or negative value fails application startup instead of only
    // surfacing as a 500 on the first real request from whichever client IP happens to be the
    // first new rate-limiter partition.
    registerPermitLimit = RateLimitingConfigurationReader.ReadPositiveIntOrDefault(
        app.Configuration, "RateLimiting:Register:PermitLimit", 5);
    registerWindowSeconds = RateLimitingConfigurationReader.ReadPositiveIntOrDefault(
        app.Configuration, "RateLimiting:Register:WindowSeconds", 300);
    loginPermitLimit = RateLimitingConfigurationReader.ReadPositiveIntOrDefault(
        app.Configuration, "RateLimiting:Login:PermitLimit", 10);
    loginWindowSeconds = RateLimitingConfigurationReader.ReadPositiveIntOrDefault(
        app.Configuration, "RateLimiting:Login:WindowSeconds", 60);
    // Defaults per the approved Messaging V1 contract (ADR-0030): 10 new conversations/hour,
    // 30 messages/minute, per authenticated user. Process-local only (System.Threading.RateLimiting
    // keeps its counters in-memory, same as the pre-existing auth-register/auth-login policies
    // above) — if TutorFlow.Host ever runs as more than one instance behind a load balancer, the
    // effective limit becomes PermitLimit x instance count, not a true shared cap. Acceptable for
    // this project's current single-instance deployment topology; a distributed counter (e.g.
    // Redis-backed) would be required before scaling horizontally, and is not built here.
    messagingCreateConversationPermitLimit = RateLimitingConfigurationReader.ReadPositiveIntOrDefault(
        app.Configuration, "RateLimiting:MessagingCreateConversation:PermitLimit", 10);
    messagingCreateConversationWindowSeconds = RateLimitingConfigurationReader.ReadPositiveIntOrDefault(
        app.Configuration, "RateLimiting:MessagingCreateConversation:WindowSeconds", 3600);
    messagingSendMessagePermitLimit = RateLimitingConfigurationReader.ReadPositiveIntOrDefault(
        app.Configuration, "RateLimiting:MessagingSendMessage:PermitLimit", 30);
    messagingSendMessageWindowSeconds = RateLimitingConfigurationReader.ReadPositiveIntOrDefault(
        app.Configuration, "RateLimiting:MessagingSendMessage:WindowSeconds", 60);

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
    else
    {
        // Dev/Testcontainers hosts run over plain HTTP; a browser that cached an HSTS policy
        // from a real deployment would otherwise refuse to load http://localhost at all.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // Serves uploaded tutor profile photos from wwwroot/uploads/tutor-photos (see
    // TutorPhotoStorage's own doc comment) — plain <img> tags load these cross-origin without
    // any CORS configuration, so this is placed independently of the CORS policy below.
    app.UseStaticFiles();

    app.UseCors(FrontendCorsPolicy);

    app.UseAuthentication();
    app.UseAuthorization();

    // Moved after UseAuthentication()/UseAuthorization() (ADR-0030) — the pre-existing
    // auth-register/auth-login policies only ever partitioned by RemoteIpAddress and never
    // needed HttpContext.User, but the new messaging-* policies above partition by the
    // authenticated caller's UserId claim, which is only populated once authentication has run.
    // Placing it here does not change auth-register/auth-login's own behavior (re-verified by
    // AuthRateLimitingTests, which exercises exactly those two routes and does not depend on
    // User being set).
    app.UseRateLimiter();

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

    // JasperFx's command-line integration (ships inside the already-referenced WolverineFx/
    // JasperFx packages — no new dependency) — required for `dotnet run -- codegen write` to be
    // reachable at all under Static TypeLoadMode (see the UseWolverine comment above). With no
    // recognized verb in `args`, this runs the application exactly as `app.Run()` did before;
    // WebApplicationFactory-based tests invoke this with an empty args array, so this preserves
    // existing test behavior unchanged.
    return await app.RunJasperFxCommands(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "TutorFlow.Host terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
