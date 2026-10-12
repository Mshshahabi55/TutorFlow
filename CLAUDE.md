# TutorFlow — Modular Monolith

## Project Context

TutorFlow is a .NET 10 modular monolith for a tutoring platform. Rich domain (scheduling conflicts, booking/payment workflows, subscription lifecycles) plus a small team pointed at a single deployable with room to extract services later. The application is composed of 14 independent modules that run in a single deployable unit (`TutorFlow.Host`) but maintain strict boundaries — each module owns its own features, data, and domain logic. Modules communicate through integration events, never by direct cross-module method calls or shared database tables.

Each module uses whatever internal architecture its own complexity justifies (run `architecture-advisor` per module if unsure):
- **Rich-invariant modules** (Bookings, Lessons, Subscriptions, Payments, Availability) — DDD tactical patterns (aggregates, value objects, domain events) once real invariants emerge. Load `ddd`.
- **Simpler modules** (Identity, Students, Tutors, Subjects, Search, Messaging, Reviews, Notifications, Learning) — Vertical Slice style (one feature, one file) until complexity proves otherwise. Load `vertical-slice`.

Don't apply DDD ceremony to a module before it has an invariant that needs protecting.

### Current state

11 of 14 modules are substantially implemented (domain models, EF Core persistence, and feature handlers): Availability, Bookings, Identity, Lessons, Notifications, Payments, Reviews, Search, Students, Subjects, Tutors. Of these, 10 also expose HTTP endpoints; Notifications is event-driven only — it consumes integration events via Wolverine and sends email, registering no routes by design.

Three modules remain empty stubs — a classlib with only `<Name>Module.cs` exposing `Add<Name>Module()` / `Map<Name>Module()`, referenced by the Host, with one passing smoke test and no persistence, feature handlers, or endpoints: Learning, Messaging, Subscriptions. Build out a stub module's first real feature via `/scaffold` when there's an actual requirement driving it, not speculatively.

## Tech Stack

- **.NET 10** / C# 14
- **ASP.NET Core Minimal APIs** — target convention: `IEndpointGroup` per feature with auto-discovery (not yet introduced; current stubs map nothing)
- **Entity Framework Core + PostgreSQL** (`Npgsql.EntityFrameworkCore.PostgreSQL`) — one `DbContext` per module, own schema. Not wired yet; add when a module's first feature needs persistence. Load `ef-core`.
- **JWT Bearer auth** — wired in `TutorFlow.Host/Program.cs` (`AddJwtBearer` + `AddAuthorization`). Dev signing key lives in user-secrets (`Jwt:Key`), never in `appsettings.json`. The Identity module owns actual user storage/token issuance when that's built.
- **Wolverine** (recommended default, not yet added) — inter-module messaging via integration events + transactional outbox, and doubles as the intra-module mediator. Add when the first cross-module event or first module with >5 features needs it. Load `messaging`.
- **FluentValidation** — request validation (add per-module as needed)
- **Serilog** — structured logging (not yet added to Host)
- **xUnit v3** + `Microsoft.Testing.Platform` + **Testcontainers.PostgreSql** — testing. `TutorFlow.Host.Tests` uses `WebApplicationFactory<Program>`.

### Package management

- **Central package management is on.** Every `PackageVersion` lives in `Directory.Packages.props`; `.csproj` files only have bare `<PackageReference Include="..." />`, no `Version`.
- **Never guess a version.** Run `dotnet add package <name>` (no `--version`) — it resolves the latest stable from NuGet and writes the entry into `Directory.Packages.props` automatically.
- Avoid MediatR, AutoMapper, and FluentAssertions v8+ — all commercial-licensed. Use `Mediator` (source-generated)/manual mapping/`Shouldly` instead.

## Architecture

```
TutorFlow.slnx
Directory.Build.props              # net10.0, Nullable, TreatWarningsAsErrors — shared by every project
Directory.Packages.props           # Central package management — all versions live here only
global.json                        # SDK pin (10.0.100, rollForward latestFeature)

src/
  TutorFlow.Host/                            # Startup host — wires all modules together
    Program.cs                              # Service registration, JWT auth, module mapping
    appsettings.json

  TutorFlow.Shared/                          # Shared kernel (thin!)
    IIntegrationEvent.cs                     # Marker interface for cross-module events

  Modules/
    Identity/ Students/ Tutors/ Subjects/ Search/ Availability/
    Bookings/ Lessons/ Subscriptions/ Payments/ Messaging/
    Reviews/ Notifications/ Learning/
      TutorFlow.Modules.[Module]/
        [Module]Module.cs                    # IServiceCollection + IEndpointRouteBuilder extensions (currently empty)
        Features/[Feature]/[Operation].cs    # target shape once features land
        Persistence/[Module]DbContext.cs     # target shape once a module needs a DB
        Consumers/                           # Wolverine/MassTransit event consumers (once messaging lands)

tests/
  TutorFlow.Host.Tests/                      # Integration — WebApplicationFactory<Program>, Testcontainers once a module has a DB
  Modules/[Module]/TutorFlow.Modules.[Module].Tests/   # One per module, mirrors src/Modules
```

### Module structure convention

Each module is a standalone class library that exposes a single registration extension. It currently does nothing; fill it in as features land:

```csharp
// src/Modules/Bookings/TutorFlow.Modules.Bookings/BookingsModule.cs
public static class BookingsModule
{
    public static IServiceCollection AddBookingsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<BookingsDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("BookingsDb")));

        return services;
    }

    public static IEndpointRouteBuilder MapBookingsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings");
        // Map booking endpoints here
        return app;
    }
}
```

### Feature file convention (once a module grows past a stub)

```csharp
public static class CreateBooking
{
    public record Command(...) : IRequest<Result<Response>>;
    public record Response(...);
    public class Validator : AbstractValidator<Command> { }
    internal class Handler(BookingsDbContext db, IMessageBus bus, TimeProvider clock)
        : IRequestHandler<Command, Result<Response>> { }
}
```

### Module communication

Modules communicate exclusively through integration events in `TutorFlow.Shared` — never by referencing another module's project, DbContext, or internal types:

```csharp
// TutorFlow.Shared/BookingConfirmed.cs — pure data, no behavior
public record BookingConfirmed(Guid BookingId, Guid StudentId, Guid TutorId, DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

// Bookings module publishes
await bus.PublishAsync(new BookingConfirmed(booking.Id, booking.StudentId, booking.TutorId, clock.GetUtcNow()));

// Notifications module consumes — convention-based handler, no interface needed
public static class BookingConfirmedHandler
{
    public static async Task HandleAsync(BookingConfirmed message, NotificationsDbContext db, CancellationToken ct)
    {
        // Handle event...
        await db.SaveChangesAsync(ct);
    }
}
```

### Database isolation

Each module that needs persistence owns its own `DbContext` targeting a separate schema:

```csharp
public class BookingsDbContext(DbContextOptions<BookingsDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bookings");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingsDbContext).Assembly);
    }
}
```

## Coding Standards

- **C# 14 features** — primary constructors, collection expressions, `field` keyword, records, pattern matching
- **File-scoped namespaces** — always
- **`var` for obvious types** — explicit types when the type isn't clear from context
- **Naming** — PascalCase for public members, `_camelCase` for private fields, suffix async methods with `Async`
- **No regions** — ever
- **No comments for obvious code** — only comment "why", never "what"
- **Module prefix** — namespace all module types under `TutorFlow.Modules.[Module]`
- **Internal by default** — module handlers, consumers, and DbContexts should be `internal` where possible; expose only the module registration extension as `public`

## Skills

Load these dotnet-claude-kit skills for context:

- `modern-csharp` — C# 14 language features and idioms
- `architecture-advisor` — choose the internal architecture for each module
- `vertical-slice` / `clean-architecture` / `ddd` — per-module internal architecture
- `project-structure` — solution layout, Directory.Build.props, central package management
- `ef-core` — DbContext patterns, query optimization, migrations (one DbContext per module)
- `messaging` — Wolverine/MassTransit, transactional outbox, integration events between modules
- `authentication` — JWT bearer, policy-based authorization
- `dependency-injection` — service registration patterns, module-scoped DI
- `error-handling` — Result pattern, ProblemDetails
- `testing` — xUnit v3, WebApplicationFactory, Testcontainers
- `configuration` — Options pattern, per-module configuration sections
- `logging` — Serilog, structured logging, OpenTelemetry
- `workflow-mastery` — parallel worktrees, verification loops, subagent patterns, context discipline

## MCP Tools

> **Setup:** Install once globally with `dotnet tool install -g CWM.RoslynNavigator` and register with `claude mcp add --scope user cwm-roslyn-navigator -- cwm-roslyn-navigator --solution ${workspaceFolder}`.

Use `cwm-roslyn-navigator` tools to minimize token consumption:

- **Before modifying a type** — `find_symbol` to locate it, `get_public_api` to understand its surface
- **Before adding a reference** — `find_references` to understand existing usage
- **To understand architecture** — `get_project_graph` to see project dependencies and verify module boundaries
- **To find implementations** — `find_implementations` instead of grep for interface/abstract class implementations
- **To check for errors** — `get_diagnostics` after changes
- **To verify module isolation** — `get_project_graph` to confirm modules do not reference each other directly

## Commands

```bash
# Start the persistent local development PostgreSQL (one-time setup: cp .env.example .env first)
# — see docs/DEVELOPMENT_DATABASE.md for credentials, migrations, and safe reset instructions.
# Automated tests never use this; they use Testcontainers, unaffected by whether this is running.
docker compose up -d

# Build entire solution
dotnet build

# Run the host (development)
dotnet run --project src/TutorFlow.Host

# Run all tests
dotnet test

# Run tests for a specific module
dotnet test tests/Modules/Bookings/TutorFlow.Modules.Bookings.Tests

# Add EF migration for a specific module (once a module has a DbContext)
dotnet ef migrations add [Name] \
  --project src/Modules/[Module]/TutorFlow.Modules.[Module] \
  --startup-project src/TutorFlow.Host \
  --context [Module]DbContext

# Format check
dotnet format --verify-no-changes
```

## Workflow

- **Plan first** — Enter plan mode for any non-trivial task (3+ steps or architecture decisions).
- **Verify before done** — Run `dotnet build` and `dotnet test` after changes. Use `get_diagnostics` via MCP to catch warnings.
- **Fix bugs autonomously** — investigate and fix without hand-holding; check logs, errors, failing tests.
- **Stop and re-plan** — if implementation goes sideways, stop and re-plan rather than pushing through.

## Anti-patterns

Do NOT generate code that:

- Defines endpoints in `Program.cs` — use per-feature endpoint mapping inside the module
- Uses `DateTime.Now` — use `TimeProvider` injection instead
- Creates `new HttpClient()` — use `IHttpClientFactory`
- Uses `async void` — always return `Task`
- Blocks with `.Result` or `.Wait()` — await instead
- Uses `Results.Ok()` — use `TypedResults.Ok()` for OpenAPI
- Returns domain entities from endpoints — always map to response DTOs
- Creates repository abstractions over EF Core — use the module's DbContext directly
- Uses in-memory database for tests — use Testcontainers
- Catches bare `Exception` — catch specific types, let the global handler catch the rest
- References another module's DbContext, project, or internal types — communicate via integration events only
- Shares database tables between modules — each module owns its schema
- Calls another module's handler directly — publish/send via the message bus for cross-module communication
- Puts business logic in `TutorFlow.Shared` — the shared kernel contains only contracts, primitives, and cross-cutting infrastructure
- Creates a single "god" DbContext for the entire application — each module gets its own
- Adds DDD ceremony (aggregates, value objects) to a module before it has a real invariant to protect
- Adds persistence, handlers, or endpoints to a module speculatively, ahead of an actual feature requirement

## Next Steps

Run `/scaffold` against whichever module has the first real feature (likely Identity for registration/login, or Subjects/Tutors for catalog data) to flesh out its first vertical slice or aggregate. Run `/health-check` periodically as modules fill in to catch drift early.
