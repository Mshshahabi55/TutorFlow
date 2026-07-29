**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-022` (all Accepted except `ADR-021`, which remains itself Proposed). This ADR records the business-owner's explicit decision (2026-07-28) to bring online-lesson delivery — via pluggable third-party meeting providers, never TutorFlow's own video infrastructure — into v1 scope, and designs the resulting architecture. It is an implementation authorization, not a design-only proposal — see Status.

---

# ADR-023: Online Lesson Meeting Provider Architecture

## Status

**Accepted — 2026-07-28.** Business decision, made explicitly by the project owner through the Constitution's Decision-Making Process (RC5.3), after being shown that SCH-1/SCH-2 and the Constitution's Project Scope explicitly excluded delivering the tutoring session itself, and choosing to widen scope rather than defer or design-only. `PROJECT_CONSTITUTION.md` Project Scope and `PRODUCT_REQUIREMENTS.md` SCH-1/SCH-2/Section 9 are amended accordingly (see those documents' own diffs, dated 2026-07-28).

## Context

An Online Session today records only a delivery mode (`SCH-2`) — no meeting link, no host/join distinction, nothing connecting a booked Session to an actual place to have the lesson. RC5.3 asks for real online-lesson delivery across three named providers (Google Meet, Microsoft Teams, Zoom) from day one, with future providers (Cisco Webex, Jitsi, BigBlueButton, ...) addable without touching Domain or Application. This ADR designs the minimum real system that satisfies that, explicitly declining to build TutorFlow's own video/conferencing stack — every provider is a third party TutorFlow only orchestrates (create/update/cancel a meeting, surface its join/host links).

## Decision

### New bounded context: Meetings

A sixth bounded context, alongside Scheduling & Booking, Identity & Relationship, Discovery, Marketplace Oversight (`ADR-002`), and Communication (`ADR-022`). Owns one aggregate: `Meeting`. Namespace/folder: `Meetings` (short form, matching `Communication`'s own one-word convention rather than the two-word "Online Lesson Delivery" title).

**Upstream/downstream:** Meetings reads Scheduling & Booking's own Session data — but only through Scheduling's already-existing `ISessionRepository` methods (`GetByIdAsync`, `GetByTutorIdAsync`, `GetByStudentIdAsync`), the same direct-cross-context-repository-read pattern RC5.1's `StartConversationCommandHandler` already established by injecting Identity's `ITutorRepository` — never a new Scheduling repository method invented for this, never a direct SQL/EF query against Scheduling's own tables from Meetings' own code. Meetings reads Communication's `IConversationRepository` the same way, for the one Conversation→Meeting linkage described below. No context depends on Meetings except Communication (for notifications, reactively) and Web (for endpoints).

### Meeting aggregate

Fields, per RC5.3's own list: `Id` (`MeetingId`), `SessionId` (`SessionId`, referenced by identity only, no FK, per `ADR-002`), `Provider` (`MeetingProviderOption` enum: `GoogleMeet`, `MicrosoftTeams`, `Zoom`), `ProviderMeetingId` (string — the id the provider itself assigned), `JoinUrl`, `HostUrl` (nullable — not every provider distinguishes a separate host link; `MockMeetingProvider` does, real providers may or may not), `StartsAtUtc`, `EndsAtUtc`, `Status` (`MeetingStatus`: `Scheduled`, `Cancelled` — a Meeting has no independent "Completed" state; the Session it belongs to already owns lesson completion, per `SCH-6`), `CreatedAtUtc`, `UpdatedAtUtc` — named `...AtUtc` to match every other timestamp field in this codebase, not RC5.3's own literal `CreatedUtc`/`UpdatedUtc` wording.

**One Meeting per Session, created on demand, never automatically at booking time.** `Meeting.Create(...)` is the only entry point; there is no automatic creation when a Session is booked or when its delivery mode is Online — a Tutor explicitly triggers creation ("Start Lesson," below). This avoids calling a third-party API (and consuming whatever quota/rate limit it has) for every Online booking regardless of whether the Tutor ever uses it, and avoids a meeting existing (and confusing a Student into thinking it's joinable) before the Tutor is ready.

`Meeting.Reschedule(newStartsAtUtc, newEndsAtUtc, nowUtc)` and `Meeting.Cancel(nowUtc)` are the only other mutations — both raise their own Domain Event (`MeetingUpdated`, `MeetingCancelled`) and are only ever invoked reactively (see "Reactive sync with Session," below), never as a direct, standalone user action — RC5.3 lists no "Cancel Lesson" or "Reschedule Lesson" UI action for any role, only Start/Join/View.

### `IMeetingProvider` — the only abstraction Application depends on

```csharp
namespace TutorFlow.Application.Meetings.Interfaces;

public interface IMeetingProvider
{
    MeetingProviderOption Provider { get; }
    Task<ProviderMeetingResult> CreateMeetingAsync(CreateProviderMeetingRequest request, CancellationToken cancellationToken = default);
    Task<ProviderMeetingResult> UpdateMeetingAsync(string providerMeetingId, UpdateProviderMeetingRequest request, CancellationToken cancellationToken = default);
    Task CancelMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default);
    Task<ProviderMeetingResult?> GetMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default);
    string GenerateJoinLink(string providerMeetingId, string storedJoinUrl);
}
```

`ProviderMeetingResult`, `CreateProviderMeetingRequest`, `UpdateProviderMeetingRequest` are plain Application-layer records (`ProviderMeetingId`, `JoinUrl`, `HostUrl`, `StartsAtUtc`, `EndsAtUtc` — no provider SDK type ever crosses this boundary). `GenerateJoinLink` is a separate, synchronous method (not folded into Create/Get) because some real providers issue per-participant signed join tokens that must be freshly generated per request rather than reused verbatim from what was stored at creation time — Google Meet/Microsoft Teams/Zoom's own implementations here return the stored link unchanged (none of the three need per-viewer signing for a basic join), but the seam exists so a future provider that does can implement it without changing the interface.

**Resolution:** a second, equally small Application port,

```csharp
public interface IMeetingProviderResolver
{
    IMeetingProvider Resolve(MeetingProviderOption provider);
}
```

implemented in Infrastructure using .NET 9 keyed DI (`AddKeyedScoped<IMeetingProvider, GoogleMeetProvider>(MeetingProviderOption.GoogleMeet)`, one line per provider, plus `AddKeyedScoped<IMeetingProvider, MockMeetingProvider>(MeetingProviderOption.Mock)` in Development only) — the resolver's own implementation is the only place that ever calls `IServiceProvider.GetRequiredKeyedService`; Application never touches the DI container directly.

### Providers implemented

`GoogleMeetProvider`, `MicrosoftTeamsProvider`, `ZoomProvider` — each a real `IMeetingProvider` implementation calling its own vendor SDK/REST API, entirely inside `Infrastructure/Meetings/Providers/`. `MockMeetingProvider` — Development-only (registered only when `ASPNETCORE_ENVIRONMENT=Development`, mirroring `DevelopmentSeeder`'s own environment guard), returns a deterministic, clearly-fake join/host URL (`https://mock-meeting.tutorflow.dev/{providerMeetingId}`) and never claims to be a real provider.

**"Provider not configured" — never a fabricated URL.** Each real provider's settings class (`GoogleMeetSettings`/`MicrosoftTeamsSettings`/`ZoomSettings`) exposes `IsConfigured` (true only when every credential it needs is present and non-empty). `CreateMeetingCommandHandler` checks `IsConfigured` (via a small `IMeetingProviderSettingsCatalog` port Infrastructure implements) before calling `CreateMeetingAsync` at all, and returns `Result.Failure(new Error("CreateMeetingCommand.ProviderNotConfigured", "This meeting provider isn't configured yet. Ask an administrator to finish setup.", ErrorType.Infrastructure))` when it isn't — never attempts the call, never invents a link. The frontend renders this specific error code as an honest "Provider not configured" state on `MeetingCard`, not a generic error banner.

### Configuration — one section per provider, no hardcoded credentials

```json
"Meeting": {
  "DefaultProvider": "GoogleMeet",
  "Google": { "ClientId": "", "ClientSecret": "" },
  "Microsoft": { "TenantId": "", "ClientId": "", "ClientSecret": "" },
  "Zoom": { "AccountId": "", "ClientId": "", "ClientSecret": "" }
}
```

Bound via `IOptions<MeetingProviderSettings>`. Every credential-shaped value ships empty in `appsettings.json` (same `REPLACE_ME`-style placeholder discipline CLAUDE.md already requires) — real values come from `dotnet user-secrets` or environment variables only, never a tracked file. `DefaultProvider` is the one non-secret value an Admin/Staff can reasonably see and is the only piece of this settings object surfaced to a "system default provider" concept (see "Provider selector," below).

### Provider selector: system default now, Tutor override designed-for-but-not-built

`CreateMeetingCommandHandler` resolves which provider to use as: **the Session's Tutor's own override, if one exists, otherwise `MeetingProviderSettings.DefaultProvider`.** Designing for the override now, without building it, means: `Tutor` (`Domain.Identity`) gains no new field in this ADR (no `PreferredMeetingProvider` column yet — that would be inventing a Tutor-specific setting RC5.3 explicitly says not to build yet); instead, `CreateMeetingCommandHandler`'s own provider-selection step is written as a single, isolated, named method (`ResolveProviderFor(TutorId)`) that today unconditionally returns the configured default — so the one and only change a future "Tutor provider override" phase needs is to make that one method read a real per-Tutor value instead of ignoring the parameter, with no change to `IMeetingProvider`, `IMeetingProviderResolver`, the `Meeting` aggregate, or any endpoint.

### Reactive sync with Session — best-effort, never blocking

Scheduling & Booking's `SessionRescheduled` gains an additive, optional trailing `NewEndTimeUtc` field (default `null`) — the same additive-enrichment pattern `ADR-022` already used for `SessionBooked`/`SessionCancelled`/`RelationshipConfirmed`; `RescheduleSessionCommandHandler` already computes the new end time from the Session's own Duration, it just wasn't on the event yet.

A new `MeetingSyncDomainEventHandler` (`Infrastructure/Meetings/`), registered as a third `IDomainEventHandler` alongside `AuditDomainEventHandler` and `NotificationDomainEventHandler` (`DomainEventDispatcher` already fans out to every registered handler — no change to that mechanism), reacts to `SessionRescheduled` and `SessionCancelled`:

1. Looks up a Meeting for the event's `SessionId` via `IMeetingRepository.GetBySessionIdAsync` (tracked — this handler mutates it). If none exists (the Tutor never started a lesson for this Session), it is a no-op — most Sessions never have a Meeting.
2. If one exists: calls `meeting.Reschedule(...)` / `meeting.Cancel(...)` — a pure, always-succeeding Domain mutation and DB write, flowing into the *same* `SaveChangesAsync` the Session mutation is already part of (same-transaction durability, the same mechanism `ADR-016` established for the audit trail).
3. **Separately, best-effort, attempts the matching `IMeetingProvider.UpdateMeetingAsync`/`CancelMeetingAsync` call, wrapped in its own try/catch that logs (`ILogger`) and swallows any exception — it is never rethrown.**

Point 3 is a deliberate, narrow departure from `ADR-012`'s Decision 3 ("a handler exception is not caught here; it propagates to `EfUnitOfWork.SaveChangesAsync`, failing the transaction"), justified because `ADR-012`'s default assumes every handler's own risk is "a local DB write can fail for catastrophic reasons" — a synchronous third-party HTTP call to Google/Microsoft/Zoom has a fundamentally different, far higher-probability failure mode (rate limits, timeouts, an expired token), and Product Goal 6 ("the platform must never lose, double-book, or silently drop a session") means a Zoom outage must never be able to block or fail a Session reschedule/cancellation that has nothing else wrong with it. The bounded, disclosed cost of this trade-off: TutorFlow's own `Meeting` row (and therefore its own UI) is always correct; the actual third-party calendar event can drift out of sync with it if the provider call fails, with no automatic retry in this ADR (a future "meeting sync health/retry" concern, not built here).

### Conversation ↔ Meeting linkage ("Join Lesson" on the Conversation page)

One new query, `GetActiveMeetingForConversationQuery(ConversationId)`, in the Meetings context. Its handler: loads the Conversation (`IConversationRepository`, cross-context read, same precedent as above) for its two participant `AccountId`s; tries each participant in turn as a `TutorId` against `ISessionRepository.GetByTutorIdAsync` (an Admin↔Admin or Student↔Student conversation legitimately has no Tutor side and correctly resolves to "no active meeting"); from whichever side resolves, filters to a `Scheduled`, `Online`-delivery Session whose other party (Student or its confirmed Parent/Guardian) is the conversation's other participant, picks the nearest one (already-ongoing first, else soonest upcoming), and looks up its Meeting. Reuses Scheduling's own existing repository methods — no new Scheduling-context code.

### Authorization

One new coarse-grained permission, `ManageMeetings`, granted to the Tutor role only (`RolePermissionCatalog`) — gates `POST /sessions/{id}/meeting` (create-or-get, "Start Lesson"). Fine-grained: the caller must be the named Session's own Tutor (`VerifyOwnTutorId`, the same helper `DeclareAvailability`/`ManageTutorOffering` already use). `GET /sessions/{id}/meeting` carries no coarse permission — `N/A (fully fine-grained)`, the same shape `GET /sessions/{id}` itself already has: the Session's own Tutor, Student, booking Parent/Guardian, or Admin/Staff (Admin: read-only, matching RC5.3's own instruction — Admin is simply never granted `ManageMeetings`).

### Notifications — reuses existing infrastructure, avoids double-notifying

Two new `NotificationType` values are wired: `MeetingCreated` (Student; Parent/Guardian if one exists — "Your online lesson meeting is ready to join," a genuinely new signal, since nothing today tells a Student a meeting now exists) and `MeetingUpdated` (same recipients — "Your online lesson meeting time has changed," filling a real gap: `SessionRescheduled` triggers no notification of any kind today). **`MeetingCancelled` is named in the enum but has no trigger wired**, the same "named but not yet meaningfully populated" pattern `ADR-022` already used for `AvailabilityChanged` — wiring it would double-notify the same real-world fact `LessonCancelled` (already raised by `SessionCancelled`) already covers, since Meeting cancellation only ever happens as that same event's own side effect; a future standalone "cancel just the meeting, keep the session" action would be the first real trigger for it. `NotificationDomainEventHandler` gains two new `case` arms (`MeetingCreated e`, `MeetingUpdated e`) — no new handler class, the same one `ADR-022` already introduced.

### Audit

`MeetingCreated`, `MeetingUpdated`, `MeetingCancelled` are all added to `AuditDomainEventHandler`'s existing switch, keyed by `MeetingId` — every one is a governance-relevant state change per CLAUDE.md's non-negotiable rule, independent of whether it also produces a Notification.

## API Shape (design-level; exact routes/DTOs finalized in implementation)

`POST /sessions/{sessionId}/meeting` (create-or-return-existing, "Start Lesson," Tutor-only), `GET /sessions/{sessionId}/meeting` (Tutor/Student/Parent/Admin, fine-grained), `GET /conversations/{conversationId}/active-meeting` (the Join-Lesson linkage). Every one gets a non-Open row in `docs/api/AUTHORIZATION_MATRIX.md` before being considered done, per CLAUDE.md's standing Definition of Done — not a new rule.

## Consequences

**Becomes possible:** a Tutor can start a real Google Meet/Microsoft Teams/Zoom meeting for an Online Session and share a real join link; a Student/Parent can join/view it; a future provider (Cisco Webex, Jitsi, BigBlueButton, ...) is addable as one Infrastructure class plus DI registration plus configuration, with zero Domain or Application changes, by construction — Application only ever sees `IMeetingProvider`.

**Becomes harder / newly constrained:** `SessionRescheduled` gains one additive, optional, trailing field. `AuditDomainEventHandler`'s switch grows by three cases; `NotificationDomainEventHandler`'s grows by two. A new, narrow exception to `ADR-012`'s "handler failure propagates" default exists for exactly one handler (`MeetingSyncDomainEventHandler`) and exactly one reason (third-party I/O), documented here rather than silently deviating.

**Now forbidden (restated, not new):** TutorFlow does not build its own video/conferencing infrastructure — every provider is a third party being orchestrated, never replaced. No fabricated meeting URL is ever shown; an unconfigured provider surfaces honestly.

## Non-Goals

Does not implement Tutor-specific provider overrides (designed for, not built — see "Provider selector"). Does not implement automatic retry/health-check for a Meeting whose provider-side sync failed (a disclosed, bounded limitation — see "Reactive sync with Session"). Does not implement recording, transcription, waiting rooms, breakout rooms, or any other provider feature beyond create/update/cancel/get/join. Does not implement Cisco Webex, Jitsi Meet, or BigBlueButton (named only as the extensibility story's own proof — no code for them exists yet). Does not change Learning Plans (`ADR-021`, still Proposed) or anything about Messages/Notifications beyond the two new `NotificationType` cases described above.

## Supersedes / Relates To

- **Amends** `PROJECT_CONSTITUTION.md` Project Scope, `PRODUCT_REQUIREMENTS.md` SCH-1, SCH-2, and Section 9 (2026-07-28) — online lesson delivery via pluggable meeting providers is brought into scope; TutorFlow's own video infrastructure and learning-materials delivery remain excluded.
- **Extends** `ADR-002` with a sixth bounded context (Meetings), the same category of extension `ADR-022` already made for Communication.
- **Reuses, does not revise,** `ADR-016`'s same-transaction durability mechanism (for Meeting's own DB writes), `ADR-003`'s coarse+fine authorization model, and RC5.1's own established precedent (`StartConversationCommandHandler`) for a context reading another context's repository directly for validation/lookup purposes.
- **Narrowly departs from** `ADR-012` Decision 3, for `MeetingSyncDomainEventHandler`'s external-provider calls only — see "Reactive sync with Session" for the full justification; every other `IDomainEventHandler` in this codebase keeps `ADR-012`'s default "failure propagates" behavior unchanged.
- **Does not touch** `ADR-021` (Learning Plans/Enrollment, still Proposed) or any Communication aggregate shape (`ADR-022`) beyond `NotificationDomainEventHandler`'s switch.

---

*Status: Accepted — 2026-07-28. Mechanism: one new aggregate (`Meeting`) in a new Meetings bounded context; `IMeetingProvider`/`IMeetingProviderResolver` ports in Application, real adapters (Google/Microsoft/Zoom) plus a Development-only Mock in Infrastructure; on-demand creation via an explicit Tutor action; update/cancel purely reactive to Session's own events, with third-party sync deliberately best-effort/non-blocking; no fabricated meeting URLs — an unconfigured provider is always honest about it.*
