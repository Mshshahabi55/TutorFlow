using Npgsql;

namespace TutorFlow.Host;

/// <summary>
/// Test-only helper (never called by production code — Program.cs reads these same five keys
/// via ordinary configuration, which in production comes from user-secrets/environment
/// variables, not this class). Exists because of a verified, structural timing conflict: for a
/// minimal-API Program.cs with no <c>CreateHostBuilder</c> method,
/// <c>WebApplicationFactory&lt;Program&gt;</c> only applies a fixture's
/// <c>ConfigureAppConfiguration</c>/<c>ConfigureWebHost</c> overrides once Program's own
/// <c>builder.Build()</c> call fires the standard "HostBuilding" diagnostic event (confirmed by
/// decompiling <c>Microsoft.AspNetCore.Mvc.Testing</c>'s <c>DeferredHostBuilder</c> and
/// <c>HostFactoryResolver</c>). <c>UseWolverine</c>'s eager <c>PersistMessagesWithPostgresql</c>
/// connection-string reads (Program.cs) run earlier than that — at the literal source line
/// <c>UseWolverine(...)</c> is called, long before <c>Build()</c> — so they never see a fixture's
/// queued override. Environment variables are visible instead, because
/// <c>WebApplication.CreateBuilder(args)</c> loads them as a default source immediately, with no
/// such deferral.
/// </summary>
public static class WolverineTestConnectionStrings
{
    private static readonly string[] Keys = ["WolverineDb", "BookingsDb", "AvailabilityDb", "TutorsDb", "PaymentsDb"];

    /// <summary>
    /// Points all five Wolverine message-store connection strings (the Host-owned Main store
    /// plus the four Ancillary stores) at real, reachable Postgres connections via process
    /// environment variables, so <c>UseWolverine</c>'s eager reads (Program.cs) see them. Call
    /// this before the fixture's first access to <c>WebApplicationFactory.Services</c> (or
    /// anything else that triggers <c>StartServer()</c>) — once the real host has started
    /// reading configuration, it is too late. Dispose the returned scope only after the
    /// fixture's own host has been stopped and disposed, since these are process-wide and a
    /// sibling fixture's host could otherwise read a value mid-restoration.
    /// </summary>
    /// <param name="defaultConnectionString">
    /// Used for any of the five keys not explicitly overridden below — the common case for a
    /// fixture whose own module isn't one of the four Ancillary stores (e.g. Subjects, Reviews):
    /// every key just needs to point at *some* real Postgres instance, and that fixture's own
    /// single Testcontainers container is as good as any other.
    /// </param>
    /// <param name="wolverineDb">Override for the Host-owned Main store's connection.</param>
    /// <param name="bookingsDb">Override for Bookings' own Ancillary store connection — pass
    /// this when the fixture already starts a dedicated Bookings Postgres container, so the
    /// outbox tables land in the SAME database as Bookings' own business tables, not a
    /// different one.</param>
    /// <param name="availabilityDb">Same as <paramref name="bookingsDb"/>, for Availability.</param>
    /// <param name="tutorsDb">Same as <paramref name="bookingsDb"/>, for Tutors.</param>
    /// <param name="paymentsDb">Same as <paramref name="bookingsDb"/>, for Payments.</param>
    public static IDisposable Configure(
        string defaultConnectionString,
        string? wolverineDb = null,
        string? bookingsDb = null,
        string? availabilityDb = null,
        string? tutorsDb = null,
        string? paymentsDb = null)
    {
        var values = new Dictionary<string, string>
        {
            ["WolverineDb"] = wolverineDb ?? defaultConnectionString,
            ["BookingsDb"] = bookingsDb ?? defaultConnectionString,
            ["AvailabilityDb"] = availabilityDb ?? defaultConnectionString,
            ["TutorsDb"] = tutorsDb ?? defaultConnectionString,
            ["PaymentsDb"] = paymentsDb ?? defaultConnectionString,
        };

        var previousValues = Keys.ToDictionary(key => key, GetEnvironmentValue);
        foreach (var (key, value) in values)
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName(key), value);
        }

        return new RestoreScope(previousValues);
    }

    /// <summary>
    /// Creates the Main and Ancillary message stores' envelope/node tables directly via SQL,
    /// matching Wolverine 6.45.0's own DDL exactly (verified by inspecting what it generates in
    /// a real Postgres instance). Required because these tables are deliberately excluded from
    /// ordinary EF Core migrations (Wolverine marks them <c>ExcludeFromMigrations()</c>) and
    /// because <c>WolverineOptions.AutoBuildMessageStorageOnStartup</c> defaults to <c>None</c>
    /// in production (correctly — no silent auto-DDL on every app start; Wolverine's own
    /// "resources setup" CLI command is the supported production provisioning step this
    /// mirrors).
    /// <para>
    /// Must be called BEFORE the host ever starts (i.e. before the fixture's first access to
    /// <c>Services</c>), not after. Verified empirically: calling Wolverine's own
    /// <c>IWolverineRuntime.Stores.MigrateAsync()</c> after the host has already started does
    /// create the tables (confirmed by querying them directly), but the durability agent's very
    /// first poll — which fires immediately on host start, before any post-startup migration
    /// call can run — still fails against the not-yet-existing schema, and every later attempt
    /// to use that store keeps failing with the same "relation does not exist" error even though
    /// the table now provably exists. Provisioning before the host starts avoids ever hitting
    /// that first failure in the first place.
    /// </para>
    /// </summary>
    public static async Task ProvisionEnvelopeTablesAsync(
        string defaultConnectionString,
        string? wolverineDb = null,
        string? bookingsDb = null,
        string? availabilityDb = null,
        string? tutorsDb = null,
        string? paymentsDb = null,
        CancellationToken ct = default)
    {
        var schemasByConnectionString = new Dictionary<string, List<string>>();
        void Add(string connectionString, string schema)
        {
            if (!schemasByConnectionString.TryGetValue(connectionString, out var schemas))
            {
                schemas = [];
                schemasByConnectionString[connectionString] = schemas;
            }

            schemas.Add(schema);
        }

        Add(wolverineDb ?? defaultConnectionString, "messaging");
        Add(bookingsDb ?? defaultConnectionString, "bookings");
        Add(availabilityDb ?? defaultConnectionString, "availability");
        Add(tutorsDb ?? defaultConnectionString, "tutors");
        Add(paymentsDb ?? defaultConnectionString, "payments");

        foreach (var (connectionString, schemas) in schemasByConnectionString)
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            foreach (var schema in schemas)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = EnvelopeTableDdl(schema);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            if (schemas.Contains("messaging"))
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = MainStoreNodeTableDdl;
                await cmd.ExecuteNonQueryAsync(ct);
            }
        }
    }

    private static string EnvelopeTableDdl(string schema) => $"""
        CREATE SCHEMA IF NOT EXISTS {schema};
        CREATE TABLE IF NOT EXISTS {schema}.wolverine_incoming_envelopes (
            id uuid NOT NULL, status varchar NOT NULL, owner_id integer NOT NULL,
            execution_time timestamp with time zone NULL, attempts integer NULL DEFAULT 0,
            body bytea NOT NULL, message_type varchar NOT NULL, received_at varchar NULL,
            keep_until timestamp with time zone NULL,
            CONSTRAINT pkey_{schema}_incoming_id PRIMARY KEY (id));
        CREATE TABLE IF NOT EXISTS {schema}.wolverine_outgoing_envelopes (
            id uuid NOT NULL, owner_id integer NOT NULL, destination varchar NOT NULL,
            deliver_by timestamp with time zone NULL, body bytea NOT NULL,
            attempts integer NULL DEFAULT 0, message_type varchar NOT NULL,
            CONSTRAINT pkey_{schema}_outgoing_id PRIMARY KEY (id));
        CREATE TABLE IF NOT EXISTS {schema}.wolverine_dead_letters (
            id uuid NOT NULL, execution_time timestamp with time zone NULL, body bytea NOT NULL,
            message_type varchar NOT NULL, received_at varchar NULL, source varchar NULL,
            exception_type varchar NULL, exception_message varchar NULL,
            sent_at timestamp with time zone NULL, replayable boolean NULL,
            CONSTRAINT pkey_{schema}_dead_letters_id PRIMARY KEY (id));
        """;

    private const string MainStoreNodeTableDdl = """
        CREATE TABLE IF NOT EXISTS messaging.wolverine_nodes (
            id uuid NOT NULL, node_number serial NOT NULL, description varchar NOT NULL,
            uri varchar NOT NULL, started timestamp with time zone NOT NULL DEFAULT now(),
            health_check timestamp with time zone NOT NULL DEFAULT now(), version varchar NULL,
            capabilities text[] NULL, CONSTRAINT pkey_wolverine_nodes_id PRIMARY KEY (id));
        CREATE TABLE IF NOT EXISTS messaging.wolverine_node_assignments (
            id varchar NOT NULL, node_id uuid NULL,
            started timestamp with time zone NOT NULL DEFAULT now(),
            CONSTRAINT pkey_wolverine_node_assignments_id PRIMARY KEY (id));
        CREATE TABLE IF NOT EXISTS messaging.wolverine_control_queue (
            id uuid NOT NULL, message_type varchar NOT NULL, node_id uuid NOT NULL,
            body bytea NOT NULL, posted timestamp with time zone NOT NULL DEFAULT now(),
            expires timestamp with time zone NULL,
            CONSTRAINT pkey_wolverine_control_queue_id PRIMARY KEY (id));
        CREATE TABLE IF NOT EXISTS messaging.wolverine_node_records (
            id serial NOT NULL, node_number integer NOT NULL, event_name varchar NOT NULL,
            timestamp timestamp with time zone NOT NULL DEFAULT now(), description varchar NULL,
            CONSTRAINT pkey_wolverine_node_records_id PRIMARY KEY (id));
        CREATE TABLE IF NOT EXISTS messaging.wolverine_agent_restrictions (
            id uuid NOT NULL, uri varchar NOT NULL, type varchar NOT NULL,
            node integer NOT NULL DEFAULT 0,
            CONSTRAINT pkey_wolverine_agent_restrictions_id PRIMARY KEY (id));
        """;

    private static string EnvironmentVariableName(string connectionStringKey) => $"ConnectionStrings__{connectionStringKey}";

    private static string? GetEnvironmentValue(string connectionStringKey) =>
        Environment.GetEnvironmentVariable(EnvironmentVariableName(connectionStringKey));

    private sealed class RestoreScope(Dictionary<string, string?> previousValues) : IDisposable
    {
        public void Dispose()
        {
            foreach (var (key, value) in previousValues)
            {
                Environment.SetEnvironmentVariable(EnvironmentVariableName(key), value);
            }
        }
    }
}
