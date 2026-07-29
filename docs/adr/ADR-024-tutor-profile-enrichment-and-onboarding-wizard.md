**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-023` (all Accepted except `ADR-003`, `ADR-020`, and `ADR-021`, which remain themselves Proposed). This ADR is a **design proposal, not an implementation authorization.** It responds to a request for a 7-step "Tutor Onboarding Wizard" (display name, headline, biography, country/city, languages, a per-tutor time zone, subjects, levels, experience, education, certifications, teaching methodology, lesson specialties, photo/video/gallery media, an hourly-rate-plus-trial-lesson pricing step with a currency selector, a weekly availability editor, an identity/education/certificate/background-check verification step, and a draft/publish lifecycle) that conflicts with several Accepted decisions and touches multiple still-open questions. This ADR does not amend any governing document and does not itself authorize writing implementation code — see Governance Note and Questions Requiring Approval.

---

# ADR-024: Tutor Profile Enrichment & Onboarding Wizard Architecture

## Status

**Accepted — 2026-07-28.** All four Questions Requiring Approval were put to the owner and each accepted as recommended: (1) self-attestation-only verification via the existing Admin approve/suspend gate, (2) the additive `TutorSubjects` collection alongside the existing `Subject` field with Discovery's search left unchanged, (3) media as plain URL fields with no upload Infrastructure in v1, (4) the `ProfileStatus` (Draft/Submitted) state machine kept fully separate from `IsApproved`/`IsSuspended`. Implementation proceeds per "Recommendation / Next Steps," below.

## Governance Note (read first)

This ADR surfaces **six** separate conflicts or open questions between the requested wizard and currently-approved documents. Two are resolved *in favor of the existing Accepted decision* (the request is adjusted, not the ADR); the rest are recorded as Questions Requiring Approval and may not be silently decided by implementation:

1. **A per-tutor "Time zone" field and a "timezone-aware schedule" conflict with `ADR-018` (Accepted).** `ADR-018` fixes `Asia/Tehran` (UTC+03:30, no DST) as the platform's single operating timezone — not a per-user setting. **Resolved in this ADR by dropping the field**: every Tutor's schedule stays Tehran-time, exactly as `DeclareAvailabilityPage`/`WeeklyAvailabilityCalendar` already work today. No code change needed here — this is a scope correction, not a design.
2. **A "Currency" selector in the Pricing step conflicts with `ADR-019` (Accepted).** `ADR-019`: *"Single currency: Iranian Rial (IRR). No multi-currency, no FX conversion, anywhere in v1."* **Resolved in this ADR by dropping the field**: trial-lesson price (if this ADR is accepted) is a second `HourlyRate`-shaped Rial amount, displayed in Toman exactly like the existing hourly rate — no currency concept is introduced.
3. **Trial-lesson pricing brushes against "Payments... out of scope for v1" (Constitution: Project Scope).** No payment is processed by declaring a trial-lesson price any more than declaring an hourly rate processes one today (`DISC-2`) — both are just numbers a Tutor sets and a Student sees. This ADR proposes trial-lesson price/availability as two more `Tutor` fields, nothing more; actually *paying* for a trial lesson is not addressed here and stays gated behind the same unresolved payments question `ADR-020`/`ADR-021` already raised.
4. **The verification step invents an answer to a genuinely open question.** `PRODUCT_REQUIREMENTS.md` Section 10.1 Item 4 / `BUSINESS_MODEL.md` Open Questions Item 6 / `DOMAIN_MODEL.md` Open Question 4: *"What information or credentials does a Tutor submit for Admin approval, and what are the Admin's approval criteria?"* This ADR proposes an answer (§"Verification", below) but it is a **recommendation, not a decision** — it is the first of the Questions Requiring Approval.
5. **"Subjects" (plural) and "Levels" touch a second open question.** `DOMAIN_MODEL.md` Open Question 10 / `PRODUCT_REQUIREMENTS.md` Section 10.3 Item 10: *"Is Subject a fixed, platform-defined taxonomy, or free text set by each Tutor?"* `TutorDto.subject` is a single string today; the wizard implies a Tutor teaches several subjects, each possibly at different levels. This ADR proposes a shape (§"Subjects & Levels", below) but again as a recommendation requiring explicit acceptance, since it changes Discovery's own filter/search semantics.
6. **No file/blob storage capability exists anywhere in this codebase.** Photo/intro-video/gallery upload has no Infrastructure to build on. This ADR proposes **not building one in v1** — see §"Media", below — rather than introducing a new external dependency (storage provider, CDN) with its own `ADR-018` reachability-from-Iran question to re-litigate.

**Before any implementation phase referenced in this ADR may begin, the owner must resolve Questions 1–4 in "Questions Requiring Approval" below.** Items 1 and 2 above need no owner decision — they are corrections, already applied throughout the rest of this document.

## Context

The five completed "Preply Redesign" UI phases deliberately built on top of the existing, narrow `Tutor` aggregate (`tutorId`, `isApproved`, `isSuspended`, `isDiscoverable`, `hourlyRate`, `subject`, `language`, `location`, `offeredDurations`) and repeatedly flagged, rather than invented, every place a richer profile (photo, bio, reviews, certificates) was missing (`docs/design/PREPLY_UX_GAP_ANALYSIS.md` §7, §22; `docs/design/DESIGN-SYSTEM.md` §9, §11). This request asks to build that richer profile's *data entry* flow — which means the aggregate itself must grow first.

## Problem Statement

Given a request for a 7-step Tutor onboarding wizard whose data model is roughly 20 fields beyond the current `Tutor` aggregate — while `ADR-018`/`ADR-019` are Accepted and must not be silently revised, while payments and a fixed subject/taxonomy model are open questions, and while no file-storage Infrastructure exists — what Domain shape, verification model, media strategy, and draft/publish lifecycle would this feature require, such that the owner can evaluate and either accept or amend it before any code is written?

## Proposal — `Tutor` aggregate additions

Every field below is additive to the existing aggregate — no existing field (`hourlyRate`, `subject`, `language`, `location`, `offeredDurations`, `isApproved`, `isSuspended`, `isDiscoverable`) is renamed, removed, or reshaped, so every already-shipped page (`TutorCard`, `TutorProfileHero`, search filters, `AUTHORIZATION_MATRIX.md` rows) keeps working unchanged.

| Field | Type | Notes |
|---|---|---|
| `DisplayName` | `string?` | The first genuine "name" field on `Tutor` — every existing page's "subject stands in for a name" convention (documented in a dozen code comments this session) becomes optional-fallback once this exists, not a rewrite. |
| `Headline` | `string?`, short | A one-line pitch, shown under the display name in the Hero. |
| `Biography` | `string?`, long text | The "About" section's real content — closes the gap `TutorProfileHero`'s own doc comment names explicitly. |
| `Country` | `string?` | New, additive — the existing `Location` field is untouched (DISC-1's search filter already depends on it; Open Question 12 on what "Location" precisely means stays open and out of scope here). |
| `City` | `string?` | Same additive treatment as `Country`. |
| `OtherLanguages` | `string[]` | The existing single `Language` field becomes, by convention, the Tutor's primary/native language; `OtherLanguages` is new, additive, never replacing it. |
| `YearsOfExperience` | `int?` | Self-declared, not verified (see Verification, below). |
| `Education` | `string?`, free text | Self-declared. A structured, repeatable "credentials" model (school/degree/year) is explicitly **not** proposed here — free text is the minimum viable shape, upgradeable later without a breaking change. |
| `Certifications` | `string?`, free text | Same reasoning as `Education`. |
| `TeachingMethodology` | `string?`, long text | Self-declared. |
| `LessonSpecialties` | `string[]` | E.g. "Exam prep," "Conversation practice" — free-text tags, not a fixed taxonomy (consistent with Subject staying unresolved, Question 5 above). |
| `PhotoUrl` | `string?` | See Media, below — a link, not an uploaded file. |
| `IntroVideoUrl` | `string?` | Same. |
| `GalleryImageUrls` | `string[]` | Same. |
| `TrialLessonAvailable` | `bool` | Default `false`. |
| `TrialLessonPrice` | `HourlyRate?` (or equivalent Rial value object) | Reuses the exact same money representation `HourlyRate` already establishes (`ADR-019`) — a second instance of the same value object, not a new one. |
| `ProfileStatus` | new enum: `Draft` \| `Submitted` | See Draft/Publish Lifecycle, below. |

## Verification (Question Requiring Approval #1)

**Recommendation:** v1 verification is **self-attestation only**, reviewed holistically by the existing Admin approve/suspend gate — no new verification sub-states.

- `YearsOfExperience`/`Education`/`Certifications` are Tutor-entered free text, exactly as `hourlyRate`/`subject` are today — TutorFlow does not verify a Tutor's claimed education any more than it verifies their claimed subject expertise today.
- **No separate "Identity status," "Education verification," "Teaching certificate verification," or "Background check status" fields are proposed.** These would each be a real new sub-domain (document upload, a human or third-party review workflow, a status enum with its own transitions and audit trail) — none of which the request's own "reuse existing APIs, add only missing endpoints" framing actually anticipated once the size of that sub-domain is made concrete. The existing `IsApproved`/`IsSuspended` gate (Admin reviews the whole submitted profile, approves or not) already *is* TutorFlow's verification mechanism; this proposal gives the Admin more information to review (the new fields above), not a new mechanism.
- **Background check integration is explicitly out of scope for v1** under this proposal — no third-party background-check provider exists in the approved-vendor list (`ADR-018`), and adding one is its own future ADR, not a placeholder field with no real behavior behind it (a placeholder status that always reads "Not started" would be exactly the kind of fabricated-looking UI this whole redesign initiative has otherwise been careful to avoid).

## Subjects & Levels (Question Requiring Approval #2)

**Recommendation:** introduce `TutorSubjects: IReadOnlyCollection<TutorSubject>` where `TutorSubject = { Subject: string, Level: string? }`, and **keep the existing single `Subject` field as-is**, populated from the first/primary entry in `TutorSubjects` for backward compatibility with every existing search/filter/card/profile call site. New Tutors fill in `TutorSubjects` during onboarding; existing Tutors' single `Subject` continues to work unchanged until they add more. `Level` stays free text (e.g., "Beginner," "Intermediate," "Advanced," "All levels") — a fixed CEFR-style taxonomy is a separate, larger decision this ADR does not make. **Discovery's search filter is unchanged in this proposal** — it continues to filter on the single `Subject` field; searching across `TutorSubjects` is a follow-on Discovery-context change, out of scope here.

## Media (resolved — no file upload Infrastructure)

`PhotoUrl`/`IntroVideoUrl`/`GalleryImageUrls` are plain URL strings the Tutor pastes in, pointing at content they already host elsewhere (the same pattern a LinkedIn/résumé profile field uses) — **not** an upload widget backed by new blob/object storage. This keeps the change entirely within the existing Web/Application/Domain layers (a URL is just a validated string) and avoids a brand-new Infrastructure dependency (a storage provider, its own `ADR-018` Iran-reachability question, a CDN, file-size/type validation, virus scanning) that a real upload capability would require. A real upload pipeline is a legitimate v2 ADR, not a detail to fold into this one.

## Draft/Publish Lifecycle

New `ProfileStatus` enum on `Tutor`: `Draft` (default on registration) → `Submitted` (Tutor explicitly submits via the wizard's final "Publish" step). `Submitted` is what enters the existing Admin approval queue (`IsApproved`/`IsSuspended` are unchanged — `ProfileStatus` gates *whether* a Tutor has asked to be reviewed at all; `IsApproved` remains the Admin's actual verdict once they have). A `Draft` Tutor is never discoverable (`IsDiscoverable` stays computed the same way it is today — `IsApproved && !IsSuspended` — `ProfileStatus` doesn't need to be added to that computation since an un-submitted profile is never `IsApproved` in the first place). This is the smallest state-machine addition that supports "incomplete profiles, draft profiles, resume later" without touching the existing approval semantics at all.

**Profile completion percentage**: computed the same way `deriveProfileCompletion`/`ProfileCompletionCard` (Phase 2, already shipped) already do — a pure frontend calculation over which fields are non-null, not a new stored field. Every new field above just adds new checklist items to that existing function.

**Autosave**: each wizard step's own existing-shaped command (see CQRS Shape, below) is called on "Next," the same as `TutorOfferingPage`'s already-established "PATCH the fields that changed, independently" pattern (`docs/phases/PHASE-01B-REPORT.md` conventions) — no new "draft blob" storage, no new persistence mechanism. "Resume later" falls out naturally: the Tutor's `Tutor` row already has whatever they saved; reopening the wizard reads it back via the existing `GET /tutors/{id}`.

## CQRS Shape (design-level — no handlers written)

New commands, one per wizard step, following the exact pattern `ManageTutorOffering`/`SetHourlyRate`/`SetSubject`/etc. already established (Application layer thin, one command per cohesive field group, each independently authorized):

- `SetTutorPersonalInfoCommand` (DisplayName, Headline, Biography, Country, City, OtherLanguages)
- `SetTutorTeachingInfoCommand` (TutorSubjects, YearsOfExperience, Education, Certifications, TeachingMethodology, LessonSpecialties)
- `SetTutorMediaCommand` (PhotoUrl, IntroVideoUrl, GalleryImageUrls)
- `SetTutorPricingCommand` (extends existing `SetHourlyRate`-shaped handler with TrialLessonAvailable/TrialLessonPrice)
- `SubmitTutorProfileCommand` (transitions `ProfileStatus: Draft → Submitted`; validates required fields are present)

Availability (Step 5) needs no new command at all — `DeclareAvailability` already exists and the wizard step is just `AddTeachingTimeDialog`/`WeeklyAvailabilityCalendar` reused in a wizard shell, per the request's own "reuse existing components" instruction.

Same `Permission` pattern as every existing Tutor-self-service command (`ManageTutorOffering`) — no new `Permission` enum value should be needed if these are gated identically; confirming that is part of implementation, not this design.

## API Shape (design-level — no endpoints implemented)

`PATCH /tutors/{id}/personal-info`, `PATCH /tutors/{id}/teaching-info`, `PATCH /tutors/{id}/media`, `PATCH /tutors/{id}/pricing` (extending the existing pricing endpoint), `POST /tutors/{id}/submit`. Every one requires its own non-Open row in `docs/api/AUTHORIZATION_MATRIX.md` before being considered done, per `CLAUDE.md`.

## Frontend Implications (design-level — no components written)

A `TutorOnboardingWizard` page reusing: the existing `Stepper`/mobile-progress pattern this session's Booking Experience phase (Phase 5) already built and documented (`DESIGN-SYSTEM.md` §12) — same sticky-actions, same focus-management-on-step-change pattern; `FormTextField`/`FormSelect`/React Hook Form + Zod, per the request's own instruction; `WeeklyAvailabilityCalendar`/`AddTeachingTimeDialog` reused verbatim for the Availability step; `ProfileCompletionCard`/`deriveProfileCompletion` extended (not replaced) for the completion-percentage requirement.

## Migration Strategy

One new EF Core migration adding the new nullable/collection columns plus the `ProfileStatus` enum column (defaulting existing rows to `Submitted`, so no currently-approved Tutor is silently un-published by this change).

## Testing Strategy (outline — no tests written)

Same three-layer discipline every prior phase in this session followed: a Domain test per new invariant (e.g., `SubmitTutorProfileCommand` rejecting submission with no `Subject`/`hourlyRate` set), an Application test per handler's authorization/orchestration, a Web test proving persistence by re-reading from a fresh `DbContext` (`CLAUDE.md`'s own non-negotiable rule).

## Consequences

**Gets easier:** the profile-richness gap this session's own UX gap analysis (`PREPLY_UX_GAP_ANALYSIS.md` §7) flagged as the single highest-priority, highest-leverage finding is finally closeable. **Becomes harder/newly constrained:** `AUTHORIZATION_MATRIX.md` gains 5 new rows; `AuditDomainEventHandler` needs new cases if any of these are modeled as Domain Events worth auditing (submission/publish likely is, per `ADR-016`; routine field edits likely aren't, matching how `TutorOfferingPage`'s existing field edits aren't audited today either).

## Supersedes / Relates To

Extends, does not supersede, the existing `Tutor` aggregate and every ADR touching it. Reuses `ADR-019`'s money representation verbatim (Trial Lesson Price). Respects `ADR-018` verbatim (no per-tutor timezone). Builds on Phase 2/4/5's own already-shipped frontend components and patterns (`docs/design/DESIGN-SYSTEM.md` §9, §11, §12).

## Non-Goals

Real file/blob upload infrastructure (v2). A fixed subject/level taxonomy (separate decision). Third-party background-check integration (separate decision, needs an approved vendor first per `ADR-018`). Any change to Discovery's search/filter behavior beyond what's stated above. Trial-lesson *payment* (still gated behind the unresolved `ADR-020`/`ADR-021` payments question).

## Questions Requiring Approval — ANSWERED 2026-07-28

1. **Verification model** — **Accepted as recommended.** Self-attestation-only, reviewed by the existing Admin approve/suspend gate. No document-verification workflow in v1.
2. **Subjects & Levels shape** — **Accepted as recommended.** The additive `TutorSubjects` collection, existing single `Subject` field kept for backward compatibility, Discovery's search filter unchanged for now.
3. **Media as URLs, not uploads** — **Accepted as recommended.** Plain URL fields; real file upload deferred to a future ADR.
4. **`ProfileStatus` state machine** — **Accepted as recommended.** Kept fully separate from `IsApproved`/`IsSuspended`.

## Recommendation / Next Steps

With all four questions accepted, next: amend `PRODUCT_REQUIREMENTS.md` (a new functional-requirements subsection, cross-referencing this ADR, resolving Section 10.1 Item 4 and Section 10.3 Item 10) and `DOMAIN_MODEL.md` (documenting the enlarged `Tutor` aggregate and closing Open Questions 4 and 10), then implement across Domain → Application → Infrastructure → Web → Frontend in that order, each with its own test coverage, exactly as every prior accepted-then-implemented phase in this session has done.

---

*Status: Accepted — 2026-07-28.*
