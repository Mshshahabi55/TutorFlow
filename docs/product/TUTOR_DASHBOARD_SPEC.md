# Tutor Dashboard — Page Specification

**Status:** Specified. Resolves `OPEN_QUESTIONS.md` A17 ("Students list"/"Insights" ownership). Not implemented.

**Route:** `/tutor/dashboard` (Tutor role only; redirects to the onboarding wizard if `TutorApplication` is not yet `Published`, per `TUTOR_ONBOARDING_WIZARD_SPEC.md` §5's permission-state rule).

No new backend module is introduced. Every section below is a read composed at the frontend/BFF layer from existing modules' own endpoints — the same "composition is a presentation-layer concern, not a module-boundary concern" pattern `ADR-0006` already established for the Classroom UI.

## Sections

| Section | Priority | Data source (module) | Primary action(s) | States |
|---|---|---|---|---|
| Today's/upcoming lessons | P0 | `Lessons` (upcoming, next 7 days) + `Bookings` (for reschedule/cancel context) | Join lesson, view booking detail | Empty: "no lessons scheduled" + CTA to share profile/check availability. Loading: skeleton list. Error: retry |
| Calendar shortcut | P0 | `Availability` (the tutor's own schedule view) | Deep-link to full Calendar page | — (deep-link only, no inline state) |
| Profile completion | P0 | `Tutors` (`TutorApplication`/`TutorProfile` completeness flags) | Resume/edit profile | Only shown while profile is incomplete or recently rejected (`TUTOR_ONBOARDING_WIZARD_SPEC.md` §5); hidden once fully complete and published |
| Unread messages | P1 | `Messaging` (unread count + most recent conversations) | Open conversation | Empty: "no messages yet." Loading: skeleton |
| Students | P1 | **Resolved (A17): `Bookings`** — distinct `StudentUserId`s with at least one `Confirmed`/`Completed` booking against this tutor, exposed via a `Bookings`-owned read endpoint (`GET /api/bookings/tutors/{tutorUserId}/students`), projected with `Students`' display data (name/photo) composed at the BFF layer | View student detail/history | Empty: "no students yet" (new tutor) |
| Earnings summary | P1 | `Payments` (balance, recent payout) | Deep-link to full Earnings/Payouts page | Empty: "no earnings yet." Payout-failure state surfaces here per `TUTOR_JOURNEY.md`'s existing error-state note |
| Insights | P2 | **Resolved (A17): composed, no owning module** — profile-view/ranking signals from `Search`, booking-conversion rate from `Bookings`, rating trend from `Reviews`. Each metric is a simple read from its owning module; there is no new aggregating backend service, consistent with `ADR-0006`'s precedent that cross-module presentation composition belongs at the BFF, not a new bounded context | View detail | "Not enough data yet" empty state for a brand-new tutor |
| Actionable tasks | P1 | Composed — e.g. "verify your certificate" (`Tutors`), "set your availability" (`Availability`), "respond to a message" (`Messaging`) | Resolve the task inline or deep-link | Hidden entirely when there are zero outstanding tasks |

## Resolution of A17

`OPEN_QUESTIONS.md` A17 asked which module owns "Students" and "Insights" as tutor-facing read projections. Resolved: **`Students`-list is `Bookings`-owned** (it already has the authoritative Student↔Tutor relationship via confirmed bookings — no new relationship needs tracking elsewhere). **`Insights` has no owning module** — it's a BFF composition of existing per-module metrics, the same architectural pattern already accepted for the Classroom UI (`ADR-0006`). Neither requires a new bounded context or backend aggregator.

## What this spec does not decide

- Exact Insights metrics/formulas (e.g. "ranking signal" definition) — deferred until `Search`'s ranking logic itself is designed (`SEARCH_SPEC.md` §8).
- Dashboard widget layout/visual design — `docs/design/` territory, not this spec.
