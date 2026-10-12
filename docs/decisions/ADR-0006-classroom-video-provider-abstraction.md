# ADR-0006: Classroom Video Provider Abstraction

**Status:** Accepted (ownership, abstraction boundary, and build-vs-buy — 2026-10-04 addendum, §"Build vs. buy" below) — concrete vendor remains deferred

## Context

The Classroom product surface needs live video, and per instruction must not be hard-coded to a single video vendor. This ADR was previously blocked on resolving which module owns the live classroom session — see `ADR-0007`-adjacent Architect question A7 and `docs/domain/DOMAIN_MODEL.md`'s former Classroom-ownership gap.

## Decision

### Classroom ownership (resolves A7)

`Lessons` owns the lifecycle of the live instructional session/classroom:

- Live lesson/session
- Classroom lifecycle
- Video/session provider abstraction

`Learning` owns durable learning artifacts — content that outlives a single session:

- Learning materials
- Homework
- Progress
- Learning plans
- Persistent instructional content

```
Lessons
   |
   +---- Live lesson/session
   |
   +---- Classroom lifecycle
   |
   +---- Video/session provider abstraction

Learning
   |
   +---- Materials
   +---- Homework
   +---- Progress
   +---- Learning plan
```

The classroom UI may aggregate information from both contexts at the presentation layer; this does not require a separate "Classroom" bounded context, and none is created.

### Video provider abstraction

Because `Lessons` owns the live classroom session lifecycle, the video/session-provider abstraction (interface) lives inside the `Lessons` module.

**The concrete vendor is not chosen and remains deferred.** Candidate providers (Daily.co, Twilio Video, Agora, LiveKit, Zoom SDK) are not evaluated or selected by this ADR — only the decision that an abstraction exists, and where it lives, is settled here.

## Build vs. buy (2026-10-04 addendum)

This ADR already settled that an abstraction exists and where it lives (above). It did not settle whether the implementation behind that abstraction is a bought video-infrastructure provider or something TutorFlow builds itself. Resolved now:

**Option A — provider abstraction (SFU-as-a-service) — adopted.** For a production European marketplace at TutorFlow's current stage, building WebRTC media-server infrastructure (SFU, TURN/STUN, recording pipeline, global edge routing) in-house is a multi-quarter undertaking with ongoing operational burden (media server scaling, codec/browser-compatibility churn) that has no connection to TutorFlow's actual product differentiation (tutoring marketplace mechanics, not video infrastructure). Every named candidate (Daily.co, Twilio Video, Agora, LiveKit Cloud, Zoom SDK) already solves this to production quality, with EU data-residency options where required. No compelling reason to build in-house was identified. Option B (build ourselves) is rejected for V1.

This does not pre-select a vendor among the candidates — that remains deferred, unchanged from the original decision — it only confirms the category (buy a provider, integrate behind `Lessons`' abstraction) rather than build.

### Session lifecycle

A classroom session's lifecycle is driven by `Lessons`' own state machine (`STATE_MACHINES.md`), not the video provider's: a room is provisioned (via the provider abstraction) when a `Lesson` transitions into its "joinable" window, and torn down (provider-side room deleted/expired) once the `Lesson` reaches a terminal state. `Lessons` never lets a provider-side session outlive its own record of the lesson.

### Join permissions

Only the `Lesson`'s own `StudentUserId`/`TutorUserId` (sourced from the `Booking` that produced it, `ADR-0016`) may obtain a join token for that session — `Lessons` issues the provider-specific join credential itself after checking the caller's identity against its own record, never delegating authorization to the video provider. A join attempt outside the lesson's active window (before it opens / after it's closed) is rejected with a clear reason, not a generic error.

### Chat, files/materials, notes — relationship to `Learning`

In-session chat is ephemeral and lives inside the video provider's own session (or a lightweight `Lessons`-owned transcript, implementation detail) — it is not the same as `Messaging`'s persistent conversations and is not synced to them. Materials/homework/notes that should outlive the session are **written to `Learning`**, not stored as part of the live session — this is the exact split `ADR-0006` already drew between `Lessons` (live) and `Learning` (durable); the classroom UI composes both at render time (unchanged).

### Screen sharing

A standard provider capability (every named candidate supports it) — exposed through the same `Lessons`-owned abstraction, no separate design needed.

### Recording policy

**Not decided here** — recording touches consent/privacy law (two-party consent considerations for an EU product) and storage-cost policy, both Product/Legal questions, not an architecture one. Added to `OPEN_QUESTIONS.md` as a new Product Owner item. The abstraction is provider-agnostic either way (most candidates support optional recording), so this does not block adopting Option A.

### Failure / reconnect behavior

A dropped connection attempts client-side rejection with the provider's own reconnect capability first; if that fails, the client re-requests a fresh join token from `Lessons` (the same join-permission check as the original join, not a weaker "reconnect" path) rather than assuming the dropped session is still valid.

## Consequences

- No separate `Classroom` bounded context is created — this was the open option explicitly rejected.
- `Lessons` defines the provider abstraction interface and owns the session lifecycle when it is scaffolded; `Learning` never needs a video dependency.
- The classroom UI composing both contexts (live session from `Lessons`, materials/homework from `Learning`) is a frontend/BFF composition concern, not a backend module-boundary concern.
- Resolves the Classroom-ownership gap previously tracked in `docs/domain/DOMAIN_MODEL.md` and Architect question A7.
- Vendor selection remains open and is not blocking this ADR's acceptance — it is decided independently when `Lessons`' classroom feature is actually scaffolded.
- **Added (2026-10-04)**: build-vs-buy is resolved (buy, Option A); session lifecycle, join-permission, chat/materials split, and failure/reconnect behavior are specified above. Recording policy is a new, explicit Product/Legal open item, not invented here.
