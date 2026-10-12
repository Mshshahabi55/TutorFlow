# ADR-0012: Booking Settings Ownership

**Status:** Accepted

## Context

`docs/product/TUTOR_JOURNEY.md` names "Booking Settings" as onboarding wizard step 13 — part of the `Tutors`-owned onboarding flow. `docs/domain/DOMAIN_MODEL.md` separately lists `BookingSettings` as a tentative key entity under `Availability`. The pre-`Availability` architecture review flagged this as a direct contradiction between two documents, left unresolved before `Availability`'s schema could safely be designed.

## Decision

**`Availability` owns the persisted `BookingSettings` entity** — minimum-notice window, maximum-booking-window, and any other rules that directly constrain slot computation and booking eligibility — not `Tutors`. This resolves the contradiction in favor of the module whose core responsibility (computing available/bookable slots) these settings directly affect, and matches `DOMAIN_MODEL.md`'s existing listing.

`Tutors`' onboarding wizard (`TUTOR_JOURNEY.md` step 13, "Booking Settings") may still present/collect this configuration as part of the onboarding UX flow, but the wizard step does not persist a `Tutors`-owned `BookingSettings` entity. When implemented, that step writes to `Availability`'s own contract (an API call or integration event — the exact mechanism is not decided here) rather than to `Tutors`' schema.

`Tutors`' existing `AvailabilitySettingsConfigured` / `BookingSettingsConfigured` boolean flags remain valid as lightweight "has the tutor completed this wizard step" markers. They do not store the actual settings data and are unaffected by this decision.

## Consequences

- `Availability`'s first implementation slice is unambiguously responsible for the `BookingSettings` entity's schema.
- **Resolved (2026-10-04)**: the wizard-step-12/13 mechanism is a direct frontend → `Availability` API call (the same self-service endpoints a tutor uses post-publish), not relayed through `Tutors`. `Tutors`' own `AvailabilitySettingsConfigured`/`BookingSettingsConfigured` flags are set by `Tutors` consuming an event `Availability` publishes on first configuration. See `docs/product/TUTOR_ONBOARDING_WIZARD_SPEC.md` §3 for the full detail.
- No change to the already-implemented `Tutors` module code is required.
- Resolves the "Booking Settings ownership" contradiction flagged in the pre-`Availability` architecture review.
