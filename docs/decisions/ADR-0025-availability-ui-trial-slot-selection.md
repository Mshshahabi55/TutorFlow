# ADR-0025: Availability UI + Trial Slot Selection

**Status:** Accepted

## Context

A ground-truth audit (2026-10-05) found that the backend "hold" mechanism this phase might have
been tempted to build **already exists, fully implemented and tested**: `Bookings.CreateTrialHold`
creates a real `Held`-status `Booking` row, protected end-to-end by a Postgres exclusion
constraint spanning `Held`/`PendingPayment`/`Confirmed`, with a lazy per-tutor expiry sweep and
conditional-`UPDATE` state transitions proven under real concurrent Testcontainers load.
`GetTrialEligibility` (`GET /api/bookings/me/trial-eligibility/{tutorUserId}`) is also fully built
and tested. **Nothing in this phase touches either of those** — they belong to the *next*
Booking/Checkout phase, which actually calls `CreateTrialHold`. This phase's job is narrower and
precedes it: let a student see real, duration-aware, authoritative availability and arrive at a
selected `(tutorUserId, duration, startUtc, endUtc)` tuple, with zero booking-creation side effects.

The actual gap, confirmed by reading the code directly: **trial duration (25/50 min) is known only
to `Bookings`**, as configuration (`BookingsOptions.AllowedTrialDurationsMinutes = [25, 50]`),
checked at hold-creation time. `Availability`'s slot-computation engine
(`AvailabilityCalculator.ComputeOpenSlotStarts`) is duration-*capable* at the primitive level
(`ValidateRequestedSlot` already accepts an arbitrary `requestedDurationMinutes` for Bookings'
own use) but the public listing endpoint, `GetTutorSlots`, hardcodes the tutor's own
`BookingSettings.DefaultSlotDurationMinutes` — there is no way today to ask "what 25-minute slots
are open," only "what slots are open at whatever duration this tutor happens to have configured
for their own calendar view." The frontend has no duration parameter anywhere, no date-range
parameter (the endpoint always returns every open slot across the tutor's *entire* booking window
in one response), and no slot-picker UI component at all — `TutorAvailability.tsx` renders a flat,
ungrouped list of all slots across all days as plain radio buttons, with a "Continue" button that
already points at `/bookings/new`, a route that doesn't exist.

## Decisions

### 1. `GetTutorSlots` evolves in place — no second slot API

`GET /api/availability/tutors/{tutorUserId}/slots` gains three new **optional** query parameters:
`durationMinutes` (defaults to the tutor's own `DefaultSlotDurationMinutes`, preserving today's
behavior for any existing caller), `fromUtc`/`toUtc` (default to `[now, now+7days]` — a one-week
window, matching the Preply benchmark's observed week-at-a-time date strip). This is additive and
backward-compatible, not a new endpoint — "if the existing API is sufficient, reuse it; if not,
evolve it" is satisfied by extending the one endpoint that already does 95% of the job, rather
than building a parallel "trial slots" API.

`AvailabilityCalculator.ComputeOpenSlotStarts` gains matching optional parameters
(`slotDurationMinutes`, `rangeStartUtc`, `rangeEndUtc`) that intersect with the existing
notice/window bounds via the exact same `if (slotStartUtc < earliestUtc || slotStartUtc >
latestUtc) continue;` check that was already there — no new computation path, no duplicated DST/
occupied-interval logic. The response gains `timeZoneId`, `durationMinutes`, and
`maxBookingWindowDays` (so the frontend can disable "next week" navigation honestly instead of
guessing) alongside the existing `slots`.

**Response-size scoping, not day-loop short-circuiting**: the day-iteration loop still walks up to
`MaxBookingWindowDays` (capped at 180 by `BookingSettings`' own validation bounds) — that's cheap
in-memory work, not a database cost. What the range parameters actually bound is (a) the *returned
payload size* (only slots inside the requested week, not the tutor's entire multi-month window)
and (b) the `IActiveBookingIntervalsQuery` call's own `fromUtc`/`toUtc`, which **is** a real DB
query and is now scoped to the requested range instead of always querying the full window. This is
the right-sized V1 optimization — further day-loop short-circuiting is a documented, not
preemptively-built, follow-up if profiling ever shows the day-loop itself matters.

`Availability` does **not** learn about "trial" as a concept and does **not** validate
`durationMinutes` against `[25, 50]` — it only validates against its own existing structural bounds
(`BookingSettings.SlotDurationMinutesLowerBound/UpperBound`, 15–240). The 25/50 policy stays
entirely in `Bookings.BookingsOptions`, exactly as domain ownership requires. The frontend is the
only place `[25, 50]` is hardcoded, matching how the product decision is stated as fixed, not
configuration-driven from the UI's perspective.

### 2. Preview vs. listing: the existing split is correct and unchanged

Guests keep getting only the anonymous coarse signal (`GetTutorAvailabilityPreview` —
`hasUpcomingAvailability`/`nextAvailableStartUtc`/`timeZoneId`); authenticated Students keep
getting the full slot listing (`GetTutorSlots`, now duration/range-aware). This exact gate
(`isAuthenticatedStudent`) is already built, tested, and approved (ADR-0022) — not revisited here.

### 3. Staleness is handled by staying honest, not by holding slots during browsing

The listing endpoint already excludes active `Held`/`PendingPayment`/`Confirmed` intervals via the
existing `IActiveBookingIntervalsQuery` — this is a **best-effort snapshot at request time**, not a
guarantee. The real authority remains `CreateTrialHold`'s own `ValidateRequestedSlot` call plus the
Postgres exclusion constraint, which the *next* phase invokes. This phase does not and must not
attempt to close that gap (e.g. by holding a slot just because it's displayed) — "the UI may have
stale data... do not solve this by holding every slot during browsing" is an explicit instruction,
and the existing architecture already embodies the correct trade-off. The frontend's job is only to
refetch on duration/date change and surface a clear error if the *next* phase's hold attempt later
fails — which is why this phase ends at slot *selection*, never slot *reservation*.

### 4. Trial eligibility is checked in the availability UI, not only in the CTA panel

`BookingPanel` already calls `GET /api/bookings/me/trial-eligibility/{tutorUserId}` to gate its own
"Book a trial lesson" button copy. `TutorAvailability` (the component that actually renders the
picker) did not call it at all — a student arriving via the `#tutor-availability` anchor or a deep
link could see a live slot picker despite having already used their trial. `TutorAvailability` now
calls the same hook (`useTrialEligibility`, React-Query-deduped against `BookingPanel`'s identical
call — no extra network request) and shows the same "already had a trial with this tutor" messaging
instead of the picker when `hasUsedTrial` is true. No new eligibility endpoint — reused as explicitly
required.

### 5. The checkout boundary is a real, honest page — not a fake booking, not a 404

`TutorAvailability`'s "Continue" button already pointed at `/bookings/new`; that route simply
didn't exist. It now does, as a real Next.js page that reads the selected
`(tutorSlug, tutorUserId, durationMinutes, startUtc, endUtc)` from the URL query string, displays
it back to the student as a plain summary, and states clearly that checkout isn't built yet — this
*is* the "honest architectural boundary, not fake checkout" the brief requires. It creates nothing:
no `Booking` row, no hold, no `CreateTrialHold` call. Carrying the selection in the URL (rather than
only in-memory component state) is the one piece of URL state this phase adds, and it's added
specifically because it's the hand-off contract to the next phase, not for its own sake — the
in-progress duration/date/time selection *within* the Tutor Profile page itself is deliberately
**not** URL-persisted (resetting on refresh is acceptable; Preply's own behavior here was
unverifiable through this phase's research, so nothing is being removed or regressed).

### 6. Component structure — composed into the existing two components, not a new monolith

The suggested `TrialDurationSelector` / `AvailabilityCalendar` (date strip) / `TimeSlotGrid` /
`SlotSelectionSummary` pieces are built as new, feature-scoped components inside
`features/tutor-profile/components/`, composed *by* `TutorAvailability` (which already owns this
concern) — not bolted onto `BookingPanel`, which stays the simple price/CTA sidebar it already is.
`ContinueToCheckout` is not a separate component — it's one `<Button>` with a navigation handler;
creating a one-line wrapper component for it would be the premature abstraction the project's own
conventions warn against. The date strip and time grid are genuinely new UI shapes (nothing in
`components/ui/` is calendar/range-shaped — the only candidate, `Select`, is single-value and not
reused here), built with the same plain-button/fieldset conventions `TutorAvailability` already
uses for its existing slot radios, not a new design-system primitive.

## Consequences

- `GetTutorSlots.Response` shape changes (gains `timeZoneId`, `durationMinutes`,
  `maxBookingWindowDays`) — additive, no existing consumer breaks since the only consumer is the
  frontend component being rewritten in the same change.
- `ComputeOpenSlotStarts`'s new parameters are optional with defaults reproducing today's exact
  behavior — `GetComputedSlots` (self-service `/me/slots`, tutor viewing their own calendar) is
  deliberately **not** changed to pass them; that endpoint keeps computing the tutor's full window
  at their own default duration, which is what a tutor managing their own calendar actually wants.
- `TutorAvailability.tsx` is restructured from a flat all-days list into a duration→date→time
  selection flow; its existing guest/not-accepting/empty states are preserved, not rebuilt.
- A new real page, `apps/web/src/app/bookings/new/page.tsx`, replaces a previously-dead route.
- No new module, no competing architecture, no new design-system primitive, no booking/hold/payment
  code written. `Tutors`, `Availability`, `Bookings` ownership boundaries unchanged.

## Noted but not fixed here

`ADR-0013` and `ADR-0014`'s own header `Status` fields still say "Proposed — requires Architect
sign-off before Bookings is scaffolded," despite both being fully implemented, tested, and in active
use (confirmed directly in code: the exclusion constraint, the lazy sweep, `ITutorAvailabilityQuery`,
`ITutorPricingQuery`, and the reciprocal `IActiveBookingIntervalsQuery` all exist exactly as
specified). This is a stale status field, not a design problem — flagged for a future doc-hygiene
pass, not silently corrected mid-phase here since it's outside this phase's actual scope.
