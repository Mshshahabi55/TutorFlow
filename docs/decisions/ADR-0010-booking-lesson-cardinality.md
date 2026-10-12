# ADR-0010: Booking ↔ Lesson Cardinality

**Status:** Accepted

## Context

`Booking` and `Lesson` are separate bounded-context concepts (`Bookings` and `Lessons` modules respectively), but their relationship was not decided — see the former Architect question A4 in `docs/OPEN_QUESTIONS.md`. This touches both modules' schemas simultaneously, so it needed resolving before either is scaffolded.

## Decision

**One confirmed/scheduled `Booking` creates exactly one `Lesson` instance.**

- `Booking` represents the commercial/reservation/scheduling lifecycle (owned by `Bookings`).
- `Lesson` represents the actual instructional session lifecycle (owned by `Lessons`).

```
Booking
   |
   +----> Lesson
```

For recurring/subscription-driven lessons, the same 1:1 rule applies **per occurrence** — a `Subscription` does not collapse into a single `Lesson`:

```
Subscription
   |
   +----> Booking #1 ----> Lesson #1
   |
   +----> Booking #2 ----> Lesson #2
   |
   +----> Booking #3 ----> Lesson #3
```

- `Subscription` is **not** itself a `Lesson`. It represents billing/entitlement/credit lifecycle only (`Subscriptions` module).
- Each scheduled occurrence — whether booked as a one-off trial or scheduled against subscription credit — gets its own `Booking` and its own `Lesson`.
- A model where one `Lesson` implicitly represents an entire recurring subscription is explicitly rejected.

## Rationale

Keeping the 1:1 rule uniform (trial or subscription-driven, it's always one `Booking` → one `Lesson`) avoids a special-cased schema for recurring lessons and keeps `Bookings`' concurrency/hold mechanism (`ADR-0003`) applicable to every scheduled occurrence, including subscription-covered ones.

## Consequences

- `Bookings` publishes an integration event (e.g. a `BookingConfirmed`-shaped contract) on confirmation; `Lessons` consumes it to create exactly one `Lesson` per confirmed `Booking`. No direct reference between the two modules' entities or database tables.
- A subscription-driven lesson still goes through the same slot-hold/booking-confirmation flow as a trial booking (per `ADR-0003`), just skipping the `Payments` step because credit already covers it — the `Booking` row still exists and still produces exactly one `Lesson`.
- This also informs (without fully resolving) the Lesson independent-state-machine question (A6) — see `docs/domain/STATE_MACHINES.md`: because `Lesson` is a distinct instance per occurrence rather than a view over a subscription, it is free to carry its own lifecycle rather than being a projection of anything else.
- Resolves Architect question A4 and the Booking↔Lesson open question raised in `docs/product/TRIAL_BOOKING_WORKFLOW.md` and `docs/product/SUBSCRIPTION_JOURNEY.md`.
- Still deferred: exact subscription cycle length, credit consumption trigger, and rollover rules (`docs/product/SUBSCRIPTION_JOURNEY.md`) — none of those are settled by this ADR.
