# Phase 0.5 Report — ADR-018: Regional Deployment & Market Scope

Branch: `develop` | Commits this phase: `324d508`, `62843fc`, plus this report's own commit

## 1. Clean Architecture validation

**Zero source files were touched.** No `.cs`, `.ts`, or `.tsx` file was modified anywhere in this phase — confirmed by `git status` immediately before each commit, showing only `docs/adr/ADR-018-*.md`, `README.md`, `CLAUDE.md`, and `PRODUCT_REQUIREMENTS.md`. Task 2's audit was read-only throughout (grep/read commands only, no edits). Task 3 found nothing in either of its two authorized-correction categories, so it made no changes either — see Section 4. This is the cleanest possible outcome for a "documentation-only phase": the absolute rule ("the ONLY source changes permitted are those explicitly authorised in Task 3, and only if Task 2 proves they are needed") was honored by finding that Task 2 proved no such need existed.

`dotnet build TutorFlow.sln` and `npm run build` + `npm run lint` were re-run after all documentation changes as a sanity check (not because any source changed) — all three remain clean: **0 Warnings / 0 Errors** (backend), clean Vite production build, zero ESLint output.

## 2. Repository convention validation

Three commits made on `develop`, each scoped to one logical unit, following the established Conventional Commits convention:

- `324d508` — `docs: add ADR-018 - regional deployment and market scope (Iran-only v1)`
- `62843fc` — `docs: propagate ADR-018 regional scope into README, CLAUDE.md, and PRODUCT_REQUIREMENTS`
- (this report, committed separately below)

No `bin/`, `obj/`, or `node_modules/` artifacts appear in any commit (unaffected by this phase — no build was re-triggered in a way that would stage new artifacts; `.gitignore` from Phase 0 already excludes them). Working tree is clean before and after each commit.

## 3. Technical debt discovered — Task 2 audit findings, by risk

### Timezone — HIGHEST RISK

- **Backend `DateTime` usage is clean.** Zero uses of `DateTime.Now`, `DateTime.Today`, or `DateTimeOffset.Now` anywhere in `backend/src` or `backend/tests`. The only local-time-shaped API in use is `DateTime.UtcNow`, at exactly 3 call sites (`src/Domain/Common/DomainEvent.cs:10`, `src/Infrastructure/Common/AuthTokenCleanupService.cs:66`, `src/Infrastructure/Common/SystemDateTimeProvider.cs:7`), all correctly UTC.
- **Every persisted/API-boundary `DateTime` is UTC by naming convention**, consistently: `Account.LockedUntilUtc`, `AuthToken.CreatedAtUtc`/`ExpiresAtUtc`/`AbsoluteExpiresAtUtc`/`RevokedAtUtc`, `AvailabilitySlot.StartTimeUtc`/`EndTimeUtc`, `Session.ScheduledTimeUtc`/`EndTimeUtc`, `DomainEvent.OccurredOnUtc`, plus the same fields mirrored in `AuditEntryDto`, `LoginResultDto`, `AvailabilitySlotDto`, `SessionDto`. Good discipline, but it is a **naming convention, not a type-system guarantee** — see next finding.
- **`DateTimeOffset` is never used anywhere in the codebase.** The team consistently chose `DateTime` + explicit "Utc" naming instead of the CLR type (`DateTimeOffset`) that would make "this value is UTC" a compile-time-checkable fact rather than a convention. Not itself broken, but weaker than it needs to be.
- **No EF Core column type is explicitly configured for any of these properties** (`grep` across all `Configurations/*.cs` found only bare `builder.Property(...)` calls, no `.HasColumnType(...)`). Storage correctness currently depends entirely on Npgsql/EF Core's default conventions plus the C# `DateTime.Kind` value at the moment of persistence — there is no database-column-level (`timestamptz`) or code-level (explicit `DateTimeKind.Utc` enforcement) backstop if a value with `Kind == Unspecified` were ever persisted.
- **Zero timezone-offset arithmetic exists anywhere** — no `TimeZoneInfo` usage, no manual `.AddHours()`-style offset math, in either backend or frontend. This means the "assumes whole-hour offsets" risk the task asked about **cannot currently manifest as a bug, because no offset conversion code exists at all** — the flip side of that same fact is the next, most severe finding.
- **The frontend performs zero timezone conversion, anywhere, for booking/scheduling times.** `frontend/src/shared/validation/isoDateTime.ts` requires the user to type a **literal UTC ISO 8601 string** (regex-enforced trailing `Z`, e.g. `2026-08-01T14:00:00Z`) directly into the Declare Availability and Reschedule Session forms — there is no date/time picker, no local-timezone input, no conversion logic. The code's own comment states this is deliberate: *"Per this sprint's instruction not to invent timezone-conversion logic... What the user types is exactly what the backend receives."* Confirmed via `grep`: zero uses of `Intl.*`, `toLocale*`, or `new Date(...)` construction anywhere in `frontend/src` (excluding tests). Read-side display is consistent with this: `SessionDetailPage.tsx`, `AvailabilitySlotDetailPage.tsx`, `StudentSessionListPage.tsx`, and `TutorSessionListPage.tsx` all render the raw UTC string with a literal `"(UTC)"` label, no conversion. **This means no user of TutorFlow — Tehran-based or otherwise — can currently book, view, or reschedule a session in their own local time; every interaction requires the user to manually compute the UTC-equivalent of `Asia/Tehran` (a non-whole-hour, +03:30 offset) themselves.** This was a reasonable, explicitly-scoped simplification when the target market was undecided; now that Iran/Tehran is formally the market (`ADR-018`), it is a severe, market-blocking UX gap, not a latent one.
- **Reasoning on backend tests under `Asia/Tehran`:** because zero code path anywhere in `backend/src` or `backend/tests` references machine-local time (`DateTime.Now`/`.Today`/`DateTimeOffset.Now` all return zero grep matches, confirmed separately for the test tree), and every timestamp flows through `DateTime.UtcNow` or an explicit UTC-suffixed field, changing the test-runner machine's system timezone to `Asia/Tehran` should have **no effect** on any test outcome — nothing in the tested code path is timezone-sensitive. This is stated as reasoning from a full-codebase grep, not from actually changing the machine timezone and re-running (forbidden by this phase's "do not change test config" instruction).

### Currency

- **`HourlyRate` (`src/Domain/Identity/ValueObjects/HourlyRate.cs`) is a bare `decimal Amount`, with no currency code anywhere in the system.** The Domain code's own comment already flags this: *"Currency/format is not established by any approved document (`DOMAIN_MODEL.md` Open Question 18) and is deliberately not modeled here."* No currency field exists on `Tutor`, `HourlyRate`, `AvailabilitySlotDto`, or anywhere else searched.
- **Column precision:** `numeric(10,2)` (`AddHourlyRatePrecision` migration, `src/Infrastructure/Persistence/Migrations/20260719215841_AddHourlyRatePrecision.cs`) — 10 total digits, 2 after the decimal point, i.e. a ceiling of 99,999,999.99.
- **Precision risk assessment:** this ceiling almost certainly has enough headroom for a realistic Toman-denominated hourly tutoring rate (plausibly in the low-hundred-thousands to low-millions range) and, since Rial = Toman × 10, for Rial-denominated rates too. **Precision overflow is not the real risk.** The real risk is twofold: (1) with no currency/unit designator stored anywhere, "is this number Rial or Toman" is ambiguous by construction — a 10x factor with no field to disambiguate it; (2) `scale=2` (i.e., cent-level precision) is functionally wasted for a currency with no practically-used minor unit, though this is inefficiency, not a correctness bug on its own.
- **Frontend:** no hardcoded currency symbol found anywhere (`TutorDetailPage.tsx:53`, `TutorDirectoryPage.tsx`, `TutorOfferingPage.tsx` all render the raw number with no `$`, no formatting, no unit label). Not a wrong-currency bug — the concept is simply absent end-to-end, consistently.

### External dependencies

- **Zero `HttpClient` usage or outbound HTTP calls found anywhere in `backend/src`.**
- **Full backend package inventory re-confirmed:** `Konscious.Security.Cryptography.Argon2` (local password hashing, no external service), `Microsoft.AspNetCore.OpenApi`, `Microsoft.Data.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL` (a database driver, host-agnostic — connects to wherever the connection string points, which can be an Iran-hosted PostgreSQL instance without modification). **Zero cloud vendor SDKs, zero payment/SMS/email service SDKs, zero Stripe/PayPal/Twilio/SendGrid references anywhere.**
- **Frontend `index.html` loads zero external resources** — no Google Fonts link, no CDN script tag; the favicon is a local `/favicon.svg`. No `@fontsource/*` package and no hardcoded Google Fonts/CDN URL found anywhere in source — MUI is running on its default system-font fallback, not pulling Roboto from a CDN (a common MUI default-setup pitfall that does **not** exist in this codebase).
- `apiClient.ts`'s axios `baseURL` resolves to the TutorFlow backend itself (`http://localhost:5046` by default, overridable via `VITE_API_BASE_URL`) — not a third party.
- **Conclusion: zero violations of Decision point 7 found anywhere in the codebase, backend or frontend.**

### i18n

- **Zero i18n/localization scaffolding found.** No `i18next`, `react-intl`, `useTranslation`, or any locale/translations directory anywhere in `frontend/src` or `package.json`. Fully consistent with Decision point 2 (English-only, no i18n) — nothing to report as drift.

## 4. Required code changes — what was changed under Task 3, and why

**None.** Task 3 authorized exactly two categories of correction, conditional on Task 2 proving they were needed:

1. *Externally-hosted frontend fonts/assets → vendor them locally.* Task 2 found **zero** externally-hosted fonts or assets anywhere in the frontend (see External Dependencies findings above) — there was nothing to vendor.
2. *`CultureInfo`-dependent parsing or formatting that would behave differently under a non-invariant culture.* A targeted `grep` across `backend/src` found **zero** uses of `CultureInfo` and **zero** uses of `DateTime.Parse`/`decimal.Parse`/`double.Parse`/`int.Parse` anywhere (all input binding goes through strongly-typed minimal-API model binding / `System.Text.Json`, which is invariant by default). The frontend independently has zero `toLocale*`/`Intl.*` usage, and its one numeric-parsing call site (`Number(values.hourlyRate)` in `TutorOfferingPage.tsx`) uses JavaScript's locale-independent `Number()` constructor, not a locale-sensitive parse.

Both authorized-correction categories resolved to "nothing found, nothing to fix." No source file was touched anywhere in this phase.

## 5. Required documentation changes

- **`docs/adr/ADR-018-regional-deployment-and-market-scope.md`** (new) — the full 8-point decision, mirroring the established ADR structure (Status/Context/Decision/Decision Rationale/Consequences/Forward Compatibility/Supersedes-Relates To/Non-Goals), modeled most closely on `ADR-017`'s "Business decision" table style since both record an owner-dictated set of sub-decisions rather than a menu of options to choose from. Explicitly notes it does not reopen `PRODUCT_REQUIREMENTS.md` Section 9's payment exclusion, and names the four forward-compatibility seams (currency, timezone conversion, payment provider, notification provider) the task required.
- **`README.md`** — new "Scope and Regional Constraints" section (6 lines), placed directly after "What TutorFlow is," linking to ADR-018.
- **`CLAUDE.md`** — the four required rules added verbatim to "Non-negotiable rules," with a pointer to ADR-018 for the full decision.
- **`PRODUCT_REQUIREMENTS.md`** — a clearly marked "Addendum: Regional Deployment & Market Scope (Appended — 2026-07-21)" section added after Section 10's Open Questions and before the final status line. Explicitly states it resolves Open Question 16 (target market) and partially informs, without resolving, Open Question 19 (jurisdiction-specific regulatory requirements). Nothing in the original numbered sections was rewritten or renumbered, per instruction.

## 6. Open decisions for the owner

Every Task 2 finding this phase was forbidden to act on, each with a proposed future phase and effort estimate:

| Finding | Severity | Proposed phase | Effort estimate |
|---|---|---|---|
| No Tehran-local timezone conversion anywhere in the frontend — users must manually type/read UTC times | **Critical (UX/market-blocking)** | Phase 1 — should be prioritized above general feature work, since the market is now formally Iran and this directly blocks usable booking for that market | 8–12 hours: a UTC↔`Asia/Tehran` conversion utility (the +03:30 offset needs correct non-whole-hour handling), a proper date/time picker replacing the raw-string input, and updated display formatting across 4 read pages. Note: this may require a date-handling package addition (e.g., a small timezone-aware library, since browser-native `Intl.DateTimeFormat` with `timeZone: 'Asia/Tehran'` may be sufficient but should be evaluated) — any new package requires its own owner sign-off per `CLAUDE.md` and is out of this phase's "no new package" rule regardless. |
| `DateTime` columns rely on convention (naming + `Kind`), not a database-enforced or type-system UTC guarantee | Medium (defensive hardening) | Phase 1 or 2 | 2–4 hours: explicit `HasColumnType("timestamptz")` (or equivalent) on the UTC-suffixed properties, plus deciding whether to enforce `DateTimeKind.Utc` explicitly at the API-boundary deserialization step. |
| Currency is entirely unmodeled — no Rial-vs-Toman disambiguation stored anywhere; `scale=2` is wasted precision for a currency with no practical minor unit | Low–Medium | Phase 1 or 2, alongside any pricing/display work | 2–3 hours: add an explicit currency/unit designator (even a single hardcoded constant resolves the ambiguity) to `HourlyRate` and its DTO/migration; separately decide whether to reduce `scale` from 2 to 0. This is a Domain and schema change requiring explicit owner approval — forbidden under this phase's absolute rules. |
| `PRODUCT_REQUIREMENTS.md` Section 10.5 Open Question 19 (jurisdiction-specific regulatory requirements) remains unresolved — ADR-018 names the market but does not enumerate Iran-specific compliance requirements | Unscored — not an engineering task | Not a phase; a research/legal task for the owner | Not estimable in engineering hours. |

## 7. Merge recommendation

**Ready to merge.** This phase's scope was strictly documentation plus a read-only audit; every absolute rule was honored (no application-behavior change, no payment module, no i18n framework, no new package, task order preserved), and the one conditional exception (Task 3's authorized corrections) resolved to "nothing to change" after a genuine, evidenced investigation rather than being skipped. `dotnet build` and the frontend build/lint remain exactly as clean as they were entering this phase, because nothing that could affect them was touched.

The substantive outcome of this phase is not code — it's the audit itself. The Critical timezone-UX finding in Section 6 is the most consequential thing this phase produced and should shape Phase 1's priority order directly; everything else found was either already clean (external dependencies, i18n, culture-dependent parsing) or a lower-severity, explicitly-scoped-out item (currency modeling, EF column typing).
