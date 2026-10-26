# Wolverine Message Storage Provisioning (A44 #106)

Deployment-provider-agnostic — this covers what any environment (local, CI, or a future
production host) needs for Wolverine's durable outbox/inbox to actually work. No production
host has been selected yet (see "What's deployment-provider-specific" below); this document is
scoped to what's true regardless of where the app eventually runs.

## Why this document exists

`docs/DEVELOPMENT_DATABASE.md` covers creating each module's own business database and applying
its EF Core migrations. It does **not** cover a separate requirement: Wolverine's own envelope
(inbox/outbox), dead-letter, and node-coordination tables. **EF Core migrations do not create
these tables** — Wolverine registers them `ExcludeFromMigrations()` by design, so `dotnet ef
database update` silently skips them. A fresh environment that only follows
`DEVELOPMENT_DATABASE.md` end-to-end will have every business table but **no Wolverine storage
at all**, and the app fails the moment anything tries to send or receive a message (`relation
"<schema>.wolverine_outgoing_envelopes" does not exist`).

This was verified directly this session (not assumed) — see "Verification performed" below.

## The five message stores

Program.cs (`PersistMessagesWithPostgresql`) registers one Main store plus one Ancillary store
per module that publishes or locally dispatches through Wolverine:

| Store | Role | Database | Schema | Config key |
|---|---|---|---|---|
| Main | `MessageStoreRole.Main` | **Its own database — never a module's** (`tutorflow_wolverine` locally) | `messaging` | `ConnectionStrings:WolverineDb` |
| Bookings | Ancillary | Bookings' own business database | `bookings` | `ConnectionStrings:BookingsDb` |
| Availability | Ancillary | Availability's own business database | `availability` | `ConnectionStrings:AvailabilityDb` |
| Tutors | Ancillary | Tutors' own business database | `tutors` | `ConnectionStrings:TutorsDb` |
| Payments | Ancillary | Payments' own business database | `payments` | `ConnectionStrings:PaymentsDb` |

An Ancillary store piggybacks on that module's own database/schema **deliberately** — it's what
lets that module's business write and its outgoing envelope share one transaction (the whole
point of A44 #103). The Main store needs a database of its own because it has no business tables
of its own and isn't owned by any single module.

The other modules (Identity/Subjects/Students/Search/Lessons/Reviews/Notifications/Messaging)
have their own databases for their own business data but are **not** Wolverine message stores —
they only ever receive messages via Wolverine's local in-process transport, which routes through
whichever store context their own consumer happens to run inside of. No separate config key is
needed for Wolverine on their account.

No credentials are listed here — see `docs/DEVELOPMENT_DATABASE.md` §2 for how connection strings
are supplied (user-secrets locally, environment variables elsewhere) and its explicit rule that a
real password never goes in a committed file.

## Fresh-environment setup, in order

1. **Create one database per module** (every entry in `DEVELOPMENT_DATABASE.md` §1's list, plus
   `tutorflow_wolverine`/your Main-store database — not created by that doc's init script,
   create it by hand the same way).
2. **Apply EF Core migrations for every module that has one** (`DEVELOPMENT_DATABASE.md` §3 —
   `dotnet ef database update` per `*DbContext`). This creates every business table. It does
   **not** create any `wolverine_*` table.
3. **Provision Wolverine's own storage** — run this repo's Host binary with the `resources setup`
   JasperFx CommandLine action, pointed at the same connection strings as step 2 (the Host already
   wires `app.RunJasperFxCommands(args)`, the same mechanism `-- codegen write` uses):

   ```bash
   dotnet run --project src/TutorFlow.Host -- resources setup
   # or, against a published build:
   dotnet TutorFlow.Host.dll resources setup
   ```

   This is the verified, correct provisioning mechanism (see "Verification performed" below) —
   not `storage setup` (that command group only has `clear|counts|rebuild|release|replay`;
   `setup` lives under `resources`, whose default action is already `setup`). It creates, per
   store: `wolverine_incoming_envelopes`, `wolverine_outgoing_envelopes`, `wolverine_dead_letters`
   in each Ancillary schema, plus the Main store's schema gets those three **and**
   `wolverine_nodes`/`wolverine_node_assignments`/`wolverine_control_queue`/`wolverine_node_records`/
   `wolverine_agent_restrictions` (node-coordination tables, Main-store-only). Safe to re-run — it
   only creates what's missing, confirmed by running it twice in a row during verification with no
   errors and no data loss the second time.
4. **Only then start the app for real traffic.** Order matters: Wolverine's durability agent polls
   immediately on host startup, before any later provisioning step could run — if the tables don't
   exist yet at that first poll, every subsequent attempt keeps failing against what is by then a
   correctly-provisioned table, because the agent's error state doesn't self-heal from a table
   showing up later (the same root cause `WolverineTestConnectionStrings.ProvisionEnvelopeTablesAsync`'s
   own doc comment documents for the test-only equivalent of this exact step).

Deliberately **not** using `WolverineOptions.AutoBuildMessageStorageOnStartup` (defaults to
`None`, left unset in Program.cs) — that would auto-run step 3's DDL on every single app start,
including in production, with no audit trail and no chance to review it first. `resources setup`
is the explicit, one-time, auditable equivalent instead.

## Verification performed (A44 #106)

Run this session against a fully disposable, throwaway Postgres container (`docker run postgres:18`
on a scratch port, **not** `tutorflow-postgres` — the persistent local dev database was never
touched, confirmed still running/healthy afterward):

1. Created 13 fresh databases (one per module + the Main store), all empty.
2. Ran `dotnet ef database update` for every module with a `DbContext` (Identity, Tutors,
   Availability, Bookings, Search, Subjects, Students, Lessons, Reviews, Payments) — confirmed
   every business table exists afterward, and confirmed **no** `wolverine_*` table exists yet on
   any of the four Ancillary schemas, and the Main store's database has no `messaging` schema at
   all. This reproduces the gap directly rather than inferring it from documentation.
3. Ran `dotnet <Host>.dll resources setup` against the same connection strings — succeeded,
   printing a per-store envelope-count table (all zero, as expected for brand-new storage) for
   all 5 stores with no errors.
4. Re-queried every schema directly via `psql`: all four Ancillary schemas
   (`tutors`/`bookings`/`availability`/`payments`) now have
   `wolverine_incoming_envelopes`/`wolverine_outgoing_envelopes`/`wolverine_dead_letters`
   alongside their pre-existing business tables; the Main store's `messaging` schema has those
   three plus all five node-coordination tables.
5. Torn down the disposable container entirely (`docker rm`); re-confirmed `tutorflow-postgres`
   (the real local dev container) was unaffected throughout.

This directly answers the open question of whether EF migrations or Wolverine's own CLI is the
right provisioning mechanism: **EF migrations are necessary but not sufficient — `resources
setup` is the additional, required step**, and it works exactly as documented above.

## Diagnosing failures

| Symptom | Cause | Fix |
|---|---|---|
| `relation "<schema>.wolverine_outgoing_envelopes" does not exist` (or `_incoming_envelopes`/`_dead_letters`) | Step 3 (`resources setup`) was never run against this environment, or was run with different connection strings than the app is actually using | Run `resources setup` with the exact same connection strings the app uses; re-check for typos between the two |
| App starts but every message silently never arrives | `resources setup` ran against the wrong database (e.g. a module's `ConnectionStrings` key points somewhere else than what `resources setup` used) | `resources` has a `check`/`statistics` action (`dotnet <Host>.dll resources check`) to confirm what the app currently sees as configured, without side effects |
| Messages stuck / repeatedly retried | Check `dotnet <Host>.dll storage counts` for dead-letter counts per store; a non-zero dead-letter count with a real exception recorded means the **handler** is failing, not provisioning — this document only covers provisioning, not handler-level bugs | `storage replay` moves dead letters back to be retried once the underlying handler bug is fixed; `storage clear` is destructive — never run it against a database with real unprocessed messages |
| `password authentication failed` | Credentials issue, not a provisioning issue | See `DEVELOPMENT_DATABASE.md`'s own troubleshooting section — identical cause/fix, not Wolverine-specific |

## What's deployment-provider-specific (explicitly out of scope here)

No production host has been selected (per the architect's own framing of this task), so the
following are intentionally **not** decided by this document:

- **When/how `resources setup` runs in a real deployment pipeline** — a one-time migration job
  step, a manual runbook step before first traffic, or something else, depends entirely on the
  eventual hosting provider's deployment model (e.g. a container platform's release-phase hook vs.
  a VM-based manual runbook). Whatever mechanism is chosen must guarantee step 3 completes before
  step 4 (first real traffic) on every fresh environment, including the very first production
  deploy.
- **Whether the Main store's database (`WolverineDb`) sits on the same physical Postgres server as
  the business databases or a separate one** — works identically either way (it's just another
  connection string); purely an ops/cost decision, not an architecture one.
- **Backup/restore cadence for `WolverineDb` relative to the four Ancillary stores' own business
  databases** — not addressed here; a restore that brings business data and envelope state out of
  sync (e.g. restoring Bookings' database to an earlier point without equally restoring its own
  envelope tables) is a real risk for a future architect decision, not resolved by this
  provisioning doc.
- **TLS, connection pooling limits, and credential-rotation mechanics** for whichever Postgres
  hosting is eventually chosen — ordinary Npgsql connection-string concerns, no different for the
  five Wolverine stores than for any other module's database.
