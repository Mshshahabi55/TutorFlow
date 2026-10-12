# Tutor Journey

## End-to-end flow

```
Apply
  → Complete onboarding wizard (17 steps)
  → Submit profile
  → Verification
  → Publish profile
  → Receive students (via Search/Messaging/Bookings)
  → Manage availability
  → Receive booking
  → Teach lesson (Classroom)
  → Complete lesson
  → Earn balance
  → Withdraw payout
  → Review insights
```

## Onboarding wizard (17 steps, as specified)

1. Account
2. Basic Information
3. Photo
4. Languages
5. Subjects
6. Specialties
7. Experience
8. Education
9. Certificates
10. Intro Video
11. Pricing
12. Availability
13. Booking Settings
14. Preview
15. Submit
16. Verification
17. Published

### Design questions this wizard raises (not decided here)

- **`[OPEN — Product Owner]`**: Is each step mandatory, or can a tutor save-and-exit partway and resume later (near-certainly yes for a wizard this long, but the exact resumability/expiry-of-draft behavior needs specifying)?
- **`[OPEN — Product Owner]`**: What exactly gates "Verification" — automated checks (e.g. certificate file present) vs. manual admin review vs. both? The brief's Admin page map includes both "Tutor Applications" and "Tutor Verification" as separate admin surfaces, implying a two-stage review — needs confirmation.
- **`[OPEN — Product Owner]`**: Can a tutor be "Published" with a pending/unverified certificate (soft launch) or is Verification strictly blocking?
- **Resolved (2026-10-04)**: wizard progress is a `Tutors` module concern — `TutorApplication` is the draft aggregate (step/status field), exactly as previously recommended, now formally adopted. See `docs/product/TUTOR_ONBOARDING_WIZARD_SPEC.md`.

### Step-content notes relevant to later modules

- Steps 4–9 (Languages, Subjects, Specialties, Experience, Education, Certificates) populate data that `Search` will later index — the Tutors↔Search integration-event contract needs to carry all of this, not just a subset.
- Step 10 (Intro Video) and the later Classroom video both touch "video" but are different concerns: intro video is an uploaded asset (storage/CDN), Classroom video is live/real-time (see `DOMAIN_MODEL.md` provider-abstraction note). Don't conflate the two when choosing providers.
- Steps 11–13 (Pricing, Availability, Booking Settings) are data the Trial Booking workflow depends on directly — see `TRIAL_BOOKING_WORKFLOW.md`. **Resolved**: step 11 (Pricing) is `Tutors`-owned data; steps 12–13 (Availability, Booking Settings) persist into `Availability`'s own entities (`AvailabilityRule`/`TimeOff`/`BookingSettings`), not `Tutors`' — `Tutors`' onboarding wizard presents these steps but does not own the persisted settings (`ADR-0012`). **Resolved (2026-10-04)**: the wizard calls `Availability`'s own self-service endpoints directly for these two steps — see `docs/product/TUTOR_ONBOARDING_WIZARD_SPEC.md` §3.

## Post-publish tutor app surfaces

Profile management, Calendar, Students, Messages, Lessons, Earnings, Payouts, Insights, Notifications, Account settings — see `NAVIGATION_AND_PAGE_MAP.md` for the per-page module mapping and open questions (Students list ownership, Insights ownership).

## Earnings → Payout

Tutor accrues balance on lesson completion (exact accrual timing — on lesson completion vs. after a dispute window — is **`[OPEN — Product Owner]`**). Payout is a `Payments` module concern via Stripe Connect (see `docs/decisions/ADR-0004-payment-provider.md`, Accepted). Payout frequency/minimum threshold/fee is **`[OPEN — Product Owner]`**.

## States to design for

- **Empty**: no students yet, no lessons yet, no earnings yet (new tutor)
- **Error**: certificate upload failure, video upload failure, payout failure (bank details invalid)
- **Permission**: unverified/unpublished tutor attempting to access post-publish-only pages (should redirect back into the wizard/status page, not show a broken empty dashboard)
- **Validation**: pricing below/above platform bounds (if any — `[OPEN — Product Owner]`), availability rule conflicts
