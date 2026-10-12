# TutorFlow Wizard Architecture Recommendation

**Status:** Recommendation / specification only. Not implemented. No backend module is proposed or changed by this document.

## Core principle

**The wizard is a frontend orchestration pattern only.** There is no `Wizard` domain module, no `WizardDbContext`, no cross-module "wizard" table. Each step writes to the backend module that already owns that data — `Identity` owns the account, `Tutors` owns profile/photo/description/pricing, `Subjects` owns the subject catalog and per-tutor subject links, `Availability` owns the weekly recurring schedule, `Bookings`/`Payments` own their own existing flows (trial booking is a separate, simpler wizard instance of the same shell). This keeps every module's data authoritative and mirrors `CLAUDE.md`'s module-isolation rule: the wizard never becomes a backend escape hatch that blurs module boundaries, and it introduces no new cross-module contract of any kind (no `ProjectReference`, no new `Shared` interface) — it is purely a client-side composition of calls to each module's own existing or future public endpoints.

This document focuses on the **tutor onboarding wizard** as the primary (P0) case, since it's the most structurally complex, and notes where the **trial booking wizard** (a much shorter instance of the same shell) differs.

## Why this matters for TutorFlow specifically

`CLAUDE.md` is explicit that modules communicate only via integration events or narrowly-scoped read-only query contracts (`ADR-0014`), never via a shared table or a god-object. A naive "wizard service" that owns draft state spanning five modules would violate that boundary the moment it tried to validate across modules server-side. The design below avoids this by keeping **all cross-step validation client-side, orchestration-only** — the backend never sees "the wizard," only ordinary per-module write calls that happen to be sequenced by the frontend.

## High-level shape

```
WizardShell (frontend-only orchestrator)
  ├─ WizardStep: Account            → Identity    (registration/login, already exists)
  ├─ WizardStep: BasicInfo          → Tutors       (name, country, location)
  ├─ WizardStep: Languages          → Tutors       (spoken languages — not yet modeled, flagged below)
  ├─ WizardStep: Subjects           → Subjects     (subject selection) + Tutors (per-subject link)
  ├─ WizardStep: Specialties        → Subjects/Tutors
  ├─ WizardStep: ProfilePhoto       → Tutors       (upload)
  ├─ WizardStep: Education          → Tutors       (repeating entries)
  ├─ WizardStep: Certifications     → Tutors       (repeating entries)
  ├─ WizardStep: Headline+Description → Tutors
  ├─ WizardStep: IntroVideo         → Tutors       (upload)
  ├─ WizardStep: Pricing            → Tutors       (feeds ITutorPricingQuery once published)
  ├─ WizardStep: Availability+Timezone → Availability (weekly recurring rules — module already modeled)
  ├─ WizardStep: Preview            → read-only aggregation of the above, client-side composition only
  └─ WizardStep: Submit             → Tutors (status: Submitted) → triggers human review workflow
```

**Gap flagged, not invented:** TutorFlow's current domain model (`DOMAIN_MODEL.md`) does not yet have a dedicated place for "spoken languages" as tutor attributes distinct from `Subjects`. This is a modeling question for whichever module ends up owning it (most likely `Tutors`, possibly a new `Languages` reference list under `Subjects`) — **flagged as `[OPEN — Architect]`, not resolved here**, since this document's job is wizard orchestration, not domain modeling.

## Draft persistence and resume

- **Server-authoritative drafts, not client-only.** Each step's data is written to its owning module as soon as the step is completed (or on a debounced autosave — see below), using the module's normal write endpoints with a `Status: Draft`/`Incomplete` marker on the `Tutor` aggregate (an attribute `Tutors` already needs regardless of the wizard, to distinguish "mid-application" from "submitted" from "approved"). This means a browser refresh or a new device login resumes exactly where the server says the tutor left off — **no separate wizard-state store is needed**, which is what keeps this frontend-only.
- **Why not pure client-side (localStorage) drafts:** they don't survive a new device/browser, don't support the "resume later" requirement across sessions, and most importantly would require the frontend to somehow re-validate against current backend state anyway before submission — better to always have the backend be the source of truth for what's actually saved.
- **Resume entry point:** the wizard's root route (`/become-a-tutor/apply`) checks the current tutor's status on load; if `Draft`/`Incomplete`, it loads already-saved step data and jumps to the first incomplete step rather than restarting at step 1.

## Step validation: client-side and server-side

- **Client-side validation** gates the "Next" button per step (matches the audit's "disabled Next state" / progressive-disclosure principle) — fast feedback, no round-trip needed for simple rules (required fields, basic format).
- **Server-side validation is still authoritative.** Each step's write call goes through that module's normal validation (FluentValidation per `CLAUDE.md`'s stack) exactly as it would for a non-wizard edit. The wizard shell must treat a server-side validation failure on "Next" as a hard stop — surfacing the server's error inline on that step, never silently advancing past it. This prevents the classic wizard bug where client-side checks pass but the record the server actually persists is invalid or incomplete.
- **Cross-module consistency is eventual, not transactional.** Because each step hits a different module, there is no single database transaction spanning "all wizard steps." This is acceptable because every step's data is independently valid within its own module the moment it's saved (e.g., an `Availability` rule set is valid on its own even before `Tutors`' profile description is filled in) — the *aggregate* "is this tutor ready to submit" check happens once, at submission time, not per-step.

## Autosave and optimistic UI

- **Autosave per step on blur/debounce** for text fields (description, headline), **immediate save on selection** for discrete choices (subjects, pricing, availability toggle) — avoids a jarring "Save" button on every step while still keeping the server authoritative.
- **Optimistic UI is safe only for same-module, non-financial steps** (profile fields, availability). It is explicitly **not** used for anything touching `Payments` or final submission — those always wait for a real server response before advancing, consistent with `CLAUDE.md`'s broader "webhook/server is authoritative, client state is never proof" philosophy already locked for the booking/payment flow (`ADR-0019`).
- **Retry on network failure:** a failed autosave shows an inline "couldn't save — retrying" state and retries with backoff; the user is never blocked from continuing to type, but "Next" remains disabled until the save actually succeeds — surfacing failure rather than silently losing data.

## Browser refresh / deep-link / auth protection

- **Browser refresh recovery:** since drafts are server-persisted (see above), a refresh mid-step reloads that step's last-saved state from the server, not from client memory. Any unsaved-in-progress keystrokes since the last autosave are the only possible loss — an acceptable, standard tradeoff given autosave is debounced in seconds, not minutes.
- **Deep-link protection:** attempting to open a later step's URL directly (`/become-a-tutor/apply/pricing`) without having completed required earlier steps redirects to the first incomplete step. This is a client-side route guard that re-checks the server-reported status on load — it cannot be bypassed by just visiting the URL, since the step's own data wouldn't exist server-side yet for the write call to succeed meaningfully anyway.
- **Unauthorized access protection:** the entire wizard sits behind the same JWT-bearer auth already wired in `TutorFlow.Host` — no wizard-specific auth scheme. A logged-out visitor hitting any wizard URL is redirected to login/registration (`Identity`), consistent with `CLAUDE.md`'s existing auth setup.

## Final submission and review/rejection feedback

- **Submission is a single explicit action** (the `ReviewStep` → `Submit`), not an automatic side effect of finishing the last field. It flips the `Tutor` aggregate's status from `Draft`/`Incomplete` to `Submitted` via `Tutors`' own endpoint — the wizard shell's only remaining job after that is to show the `CompletionState` screen.
- **Review outcome delivery:** per the audit, Preply communicates outcome via email after an offline review process; TutorFlow should do the same functionally (an `Identity`/`Notifications`-delivered email, using the existing `TutorFlow.Shared` integration-event pattern — e.g. a `TutorApplicationSubmitted` event consumed by `Notifications`) plus an in-app status badge (`Under Review`/`Approved`/`Rejected`) visible whenever the tutor returns to their dashboard. **This reuses the existing event-bus pattern already established for cross-module notification (mirrors `BookingConfirmed` → `Notifications` in `CLAUDE.md`'s own example) — no new mechanism is invented.**
- **Rejection feedback:** if rejected, the tutor should be able to re-enter the wizard, see what was rejected (reason text, if `Tutors` stores one), and resubmit — this is the same `Draft` resume path, not a new flow.

## Responsive/mobile UX

- One step per screen on mobile (already the natural shape of a wizard step); the sticky `WizardFooter`/`StepNavigation` becomes a fixed bottom bar on mobile rather than inline, matching common mobile wizard convention — **this is a TutorFlow design decision, not a copied Preply behavior**, since Preply's own mobile step *ordering* differs from its web ordering for the (much shorter) booking flow, which TutorFlow is not required to replicate.
- File/video/image upload steps need explicit mobile camera-capture affordances (`<input type="file" capture>` or equivalent), since tutors applying from a phone are a realistic case even if TutorFlow's own desktop-first MVP prioritizes web.

## What this architecture deliberately does NOT do

- No new backend module, `DbContext`, or schema for "the wizard" itself.
- No new cross-module `ProjectReference` or command-style `Shared` contract — every step talks to exactly one module via that module's own public API, the same way any other client of that module would.
- No change to any module's existing domain rules to make the wizard "feel smoother" — e.g., `Availability`'s weekly-recurring-rules model is used as-is, not reshaped for wizard convenience.
- No assumption that TutorFlow must match Preply's exact 8-section breakdown — the step list above is a reasonable mapping to TutorFlow's actual module boundaries, not a copy of Preply's internal grouping (which itself was only partially observable — see `PREPLY_UI_UX_AUDIT.md` §27.2).

## Open items requiring Product Owner / Architect decision

1. Where "spoken languages" live in the domain model (`Tutors` attribute vs. new `Subjects`-adjacent reference list) — flagged above, not resolved here.
2. Whether draft `Tutor` rows are visible/searchable before approval (almost certainly no, but not yet an explicit rule anywhere in `DOMAIN_MODEL.md`).
3. Whether rejection is final or resubmission-eligible, and whether there's a cooldown — not specified anywhere yet.
4. Exact review-queue mechanism for human reviewers (admin tooling) — out of scope for this document, but a real prerequisite before `Submitted` status means anything operationally.
