# ADR-0022: Tutor Profile Public Surface — Slug Identity, Accepting-Students State, Trial Tracking

**Status:** Accepted

## Context

The Tutor Profile feature (`docs/design/PREPLY_TUTOR_PROFILE_PARITY_SPEC.md`) needed a public,
anonymous-accessible way to view a Published tutor, a human-friendly public URL that doesn't
expose the internal `UserId` (ADR-0011's cross-module identity, never meant to be a URL), a way
to represent Preply's observed "tutor isn't accepting new students" state without collapsing it
into "not found," and a way for the Booking CTA to know whether a student has already used their
one trial with a given tutor. None of these existed before this feature; all four are minimal,
additive domain changes, not a new module or a new cross-module identity concept.

## Decisions

### 1. `Tutors` owns a public routing slug, distinct from `UserId`

`TutorApplication.PublicSlug` (string, unique, nullable until first submission) is assigned once,
inside `Submit()`, as a pure domain operation: `{slugify(DisplayName)}-{Id:N[..8]}`. The
`Id`-derived suffix makes it unique by construction — no database uniqueness check or retry logic
is needed anywhere, and the same operation is safe to call from any `Submit()` caller (the HTTP
handler, or a test fixture that calls the domain method directly), not just one code path that
remembers to do it.

This does **not** change ADR-0011: `UserId` remains the only cross-module tutor identity used in
API payloads and inter-module contracts. `PublicSlug` is purely a Tutors-owned, publicly-routable
addition for the `/tutors/{slug}` URL. Rejected: using `UserId` directly in the URL (a Guid is not
what a user should ever need to read or type, even though they never type it directly — see
§5 of the parity spec) and a classic incrementing-integer-with-collision-retry slug (unnecessary
complexity given the Id-suffix approach is already collision-free).

Frontend/API composition still uses `UserId` for every subsequent call (availability, trial
eligibility, favorites) — the slug only ever appears in the URL and the profile response's own
`slug` field; it is never propagated as a lookup key into other modules.

### 2. `Tutors` owns a tutor-controlled "accepting new students" flag, independent of `Status`

`TutorApplication.IsAcceptingNewStudents` (bool, default `true`) is a new, orthogonal field —
**not** a new `Status` enum value and not the same concept as `Suspend()` (an admin/platform
action). A `Published` tutor with `IsAcceptingNewStudents = false` still returns `200` from the
public profile endpoint with every section intact; only the booking CTA reflects it. This matches
the one real observed Preply behavior (a profile that's "not accepting new students" renders
fully, only the CTA swaps) more closely than collapsing every non-default state into a 404 would.

No HTTP endpoint sets this yet — deliberately. Building a tutor-facing self-service toggle
endpoint is out of this feature's scope (see the completion report's "remaining dependencies");
the field exists so the state is representable now rather than retrofitted later, without
inventing a toggle endpoint nobody asked for yet ("don't silently create a large new lifecycle").

Every other non-`Published` status (`Draft`/`Submitted`/`UnderReview`/`Approved`/`Rejected`/
`Suspended`) is still treated identically to "not found" (404) by the public profile endpoint —
internal pipeline state is never leaked publicly. This is intentionally stricter than Preply's
own observed behavior for its one "not accepting" case, which is a genuinely different state
(still published, still searchable) from being unpublished entirely.

### 3. `Bookings` owns a `Booking.IsTrial` flag

Every booking created today goes through `CreateTrialHold` — the only creation path that exists
in this module (no regular/subscription-lesson booking exists yet). `IsTrial` (bool, set `true`
at creation, carried through `RescheduleBooking`'s linked replacement) makes this explicit rather
than implicit, so the new `GetTrialEligibility` read (`GET /api/bookings/me/trial-eligibility/
{tutorUserId}`) has a real field to query. Without this flag, the moment a second creation path
(regular lessons) exists, "has this student used their trial" would become unanswerable from
existing data.

### 4. `Availability` exposes a second, anonymous-access read: `GetTutorAvailabilityPreview`

`GET /api/availability/tutors/{tutorUserId}/preview` (no auth) answers only "does this tutor have
upcoming availability, and when's the soonest" — never the full slot grid, which stays behind
`GetTutorSlots`' existing authenticated-only access. This lets Guests see a genuinely useful
availability signal on the public profile (per explicit product direction: "Guests must be able
to view useful tutor availability information/preview without being authenticated... Authentication
is required when the user actually needs to perform a protected action") without exposing the
same level of detail an authenticated, booking-intent user gets. Reuses the exact same
`AvailabilityCalculator` the full endpoint already uses — only the response shape differs.

### 5. `Students` implements its already-decided `FavoriteTutor` ownership

Ownership was already settled by precedent (`PREPLY_SEARCH_PARITY_MATRIX.md` row 28: "Students
(`FavoriteTutor`, stub)") — this feature is that capability's first real implementation, not a
new ownership decision. `PUT`/`DELETE`/`GET /api/students/me/favorites/{tutorUserId}`, each
idempotent, matching a binary favorited/not-favorited heart-icon toggle rather than a counter or
history.

### 6. Composition stays at the frontend; no new backend aggregation module

The Tutor Profile page composes `GET /api/tutors/{slug}` (core profile), the existing public
catalog endpoints (subject/specialty/language name resolution — already the frontend's job per
ADR-0021), the new availability preview, the new trial-eligibility read, and the new favorite
status, via parallel frontend fetches — not a new backend BFF/aggregator module. This matches
ADR-0021's own precedent exactly (ID→name resolution is the frontend's job, not the API's) and
avoids both failure modes named during planning: no god endpoint, no frontend N+1 waterfall
(each read is independent and parallelizable; `useCatalog()` is already a one-time cached fetch
shared app-wide, not fetched per profile view).

### 7. No new module

`TutorProfile` is a page composed from five existing modules (`Tutors`, `Subjects`, `Availability`,
`Bookings`, `Students`) plus one future one (`Reviews`, still a stub — see below). Source-of-truth
ownership for every field is unchanged; nothing moved between modules.

## Explicitly deferred (not decided by this ADR)

- **Reviews**: ~~still a pure stub~~ — **implemented in a later session**, per an explicit Product
  Owner decision to build the documented Preply model (Student↔Tutor relationship, eligible at
  the 3rd completed lesson with that tutor, one editable review per relationship — never a
  per-Lesson review). `GET /api/reviews/tutors/{tutorUserId}/summary` exists close to, but not
  identical to, the shape sketched in `TutorReviews.tsx`'s original comment — no `authorName`
  field (no safe, non-fabricated source for it), `subRatings` kept nullable and never populated
  (no submission-side evidence exists). See `docs/product/preply-reverse-engineering/12-classroom-
  messaging-reviews-notifications.md` for the full evidence/parity writeup.
- **Messaging**: still a stub. The Message CTA is real for authenticated Students (navigates to
  `/messages`, which today renders the same `ComingSoon` pattern every other unbuilt page in this
  app already uses) and gated behind the auth prompt for Guests — never a fabricated send.
- **Dynamic Super Tutor / Professional Tutor computation**: both remain admin/platform-set
  booleans (already modeled before this feature). Preply's actual trailing-90-day, 9-metric
  computation and course/certificate-based eligibility are not implemented.
- **Session/auth state in the frontend**: no login/token-storage mechanism exists anywhere in
  `apps/web` yet (`/login`, `/signup` are themselves `ComingSoon` stubs). `useAuth()` is written
  against the correct final shape (`isAuthenticated`, `userId`, `role`) but always returns the
  signed-out state today — every Tutor Profile visitor is treated as a Guest in practice until a
  real session store exists. No Tutor Profile code changes will be needed when one does; only
  `useAuth()`'s implementation will.
