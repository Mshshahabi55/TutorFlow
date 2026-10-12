# UX States — Cross-Cutting Policy

Every screen/component built against this spec must account for these states from the start, not as a later polish pass. This is a policy document; per-screen specifics live in the relevant journey doc (`docs/product/*_JOURNEY.md`, `TRIAL_BOOKING_WORKFLOW.md`).

## The seven states

| State | Definition | Policy |
|---|---|---|
| **Loading** | Data/action in flight | Skeletons for content-shaped loads (lists, profiles); spinners/disabled-button-with-spinner for action-shaped loads (submit, pay). Never a blank screen with no feedback. |
| **Empty** | Zero data, not an error | Must explain *why* it's empty and what to do next (e.g. "No tutors match these filters — try widening your search"), not just "No results." |
| **Error** | Something failed | Must distinguish retryable (network blip — offer retry) from non-retryable (payment declined — explain why, offer a different path) wherever the underlying system can tell the difference. Never show a raw exception/stack trace to an end user. |
| **Validation** | Input doesn't satisfy a rule before submit | Inline, next to the offending field, in plain language tied to the actual rule (e.g. a slot-selection validation should name the real constraint — min notice, max window — from `Availability`, not a generic "invalid selection"). |
| **Success** | Action completed | Explicit confirmation, not just a silent state change — especially for money-moving actions (payment, refund, payout). |
| **Disabled** | Action not currently available | Must be distinguishable from "permission denied" — a disabled "Book Trial" button because the tutor has no open slots this week reads differently than one disabled because the viewer isn't logged in. Don't collapse both into the same visual/copy treatment. |
| **Permission** | Viewer lacks access | Redirect to the right next step (login, upgrade, apply) rather than a dead-end 403. See `docs/product/ROLES_AND_PERMISSIONS.md` for who can do what. |

## Why this is called out separately from the journey docs

The brief is explicit: "Do not wait until the end to think about these states." Each journey doc (`STUDENT_JOURNEY.md`, `TUTOR_JOURNEY.md`) already lists its own empty/error/permission/validation cases inline for that reason. This document exists so there's one place defining what "handled" means for each state category, so per-screen call-outs don't each invent their own standard.

## Explicit non-decisions

Exact copy/wording for any state message is not specified here — that's a content/UX-writing decision, and per the brief, TutorFlow must use its own original copy, never Preply's.
