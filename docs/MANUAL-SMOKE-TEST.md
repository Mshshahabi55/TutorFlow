# Manual Smoke Test — Browser, End to End, Real PostgreSQL

A copy-pasteable walkthrough you run yourself in a browser, against the real
API and real PostgreSQL — not a test suite. It proves the whole stack works
together, and specifically proves the Phase 1B persistence defect (Approve
silently not saving) is really gone by checking a page **refresh**, not just
a passing test.

Budget ~15 minutes. You'll play four roles in sequence (Admin, Tutor,
Student) by logging out and back in with different seeded accounts.

## 1. Start the backend and the frontend

Two terminals.

**Terminal 1 — backend**, from the repo root:

```bash
cd backend/src/Web
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

Wait for `Now listening on: http://localhost:5046` (or whatever port your
`launchSettings.json`/`--urls` resolves to — the frontend's default
`VITE_API_BASE_URL` fallback is `http://localhost:5046`, so use that unless
you've overridden it). If this is a fresh checkout, complete "Database
setup" in `README.md` first (create the databases, `dotnet user-secrets
set "ConnectionStrings:TutorFlow" ...`, `dotnet user-secrets set
"Seed:AdminPassword" ...`).

**Terminal 2 — frontend**, from the repo root:

```bash
cd frontend
npm install
npm run dev
```

Open the URL it prints (default `http://localhost:5173`).

## 2. The seeded accounts

On startup, the backend seeds (idempotently — safe if you restart it) four
accounts, defined in `backend/src/Web/DevelopmentSeeder.cs`:

| Role | Email | Password |
|---|---|---|
| Admin/Staff | `admin@tutorflow.dev` | whatever you set as `Seed:AdminPassword` |
| Tutor (already approved, pre-configured offering) | `tutor@tutorflow.dev` | `Seed-Password-123!` |
| Student | `student@tutorflow.dev` | `Seed-Password-123!` |
| Parent/Guardian | `parent@tutorflow.dev` | `Seed-Password-123!` |

You won't need the seeded Tutor account below — this walkthrough registers
a **new** Tutor from scratch so you can watch Approve take effect on an
account that starts out pending. The seeded Tutor/Student/Parent-Guardian
accounts are there as a fallback if you want to explore further afterward.

If you don't know the Admin password because you didn't set
`Seed:AdminPassword` yourself, set it now (`dotnet user-secrets set
"Seed:AdminPassword" "<a-password-you-choose>"` from `backend/src/Web`) and
restart the backend — seeding is idempotent, so this is safe even if the
Admin account already exists (it won't be recreated, but you now know its
password since you just set it before the account existed the first time;
if the account already existed from an earlier run with a different
password, delete that one row from `AdminStaffs` and restart, or just use
whatever password you originally set).

## 3. Log in as Admin

Go to `http://localhost:5173/auth/login`. Enter `admin@tutorflow.dev` and
your Admin password.

**Expect:** you land on the app shell with a left navigation sidebar. The
sidebar shows sections for every bounded context (Identity & Relationship,
Scheduling & Booking, Discovery, Marketplace Oversight) — as an Admin you
should see entries including "Pending Tutors" and "All Sessions". If login
fails, double check the password matches exactly what's in
`Seed:AdminPassword`.

## 4. Register a new Tutor, approve it, and prove the approval survives a refresh

This is the step that matters most — it's the user-visible face of the
defect Phase 1B fixed (`TutorRepository.GetByIdAsync`/`GetByEmailAsync`
silently not persisting `Approve()`, `Suspend()`, and every other Tutor
mutation, while still returning success).

1. Navigate to **Register Tutor** (`/identity/tutors/register`). Enter any
   email (e.g. `smoke-tutor@example.com`) and a password meeting the usual
   rules (e.g. `Smoke-Password-123!`). Submit.
   **Expect:** a success message showing the new Tutor's id, with a copy
   button. **Copy this id** — you'll need it repeatedly below. Call it
   `TUTOR_ID`.
2. Navigate to **Pending Tutors** (`/identity/tutors/pending`, still logged
   in as Admin). **Expect:** your new Tutor listed, since it isn't approved
   yet.
3. Click the row's **Approve** action and confirm the dialog.
   **Expect:** a success notification.
4. **Now refresh the entire browser page (F5 / Cmd+R) — not a soft
   client-side navigation, an actual full reload.** Go back to Pending
   Tutors.
   **Expect: the Tutor you just approved is GONE from this list.** If it's
   still there after a hard refresh, the approval didn't really persist —
   that would mean the Phase 1B defect (or something like it) is back, and
   you should stop and report it rather than continuing this walkthrough.
5. Navigate directly to the Tutor's detail page
   (`/identity/tutors/TUTOR_ID`, substituting your copied id).
   **Expect:** the page shows `isApproved: true` (or an equivalent "Approved"
   status pill). This is a **fresh page load reading from the database**,
   not a cached response from step 3 — exactly the check that would have
   failed before the Phase 1B fix.

## 5. Set the Tutor's offering and declare availability

Still as Admin, or log out and log back in as the new Tutor account
(`smoke-tutor@example.com`) — either works for these steps depending on
what your current role's permissions allow; the Tutor's own offering fields
require the Tutor to be logged in as themselves.

1. Log out, log back in as `smoke-tutor@example.com` / the password you
   registered with.
2. Navigate to the Tutor's offering page
   (`/identity/tutors/TUTOR_ID/edit`). Set a Subject (e.g. `Mathematics`),
   Language, Location, and **Hourly rate (Toman)** — e.g. type `50000` for
   50,000 Toman. **Expect:** each field saves independently with a success
   notification. Under the hood, what's actually sent and stored is
   500,000 Rial (1 Toman = 10 Rial, ADR-019) — you never compute that
   conversion by hand, the picker does it for you, same as the Tehran time
   picker below.
3. Navigate to **Declare Availability**
   (`/scheduling/availability/declare`).
   - **Tutor id**: `TUTOR_ID`
   - **Start time (Tehran)**: use the date/time picker to choose **5:30 PM
     on August 1, 2026** — enter it exactly as you'd say it out loud in
     Tehran; the picker converts it to UTC for you before the request is
     sent, so you never compute an offset by hand.
   - **Duration (minutes)**: `60`
   - **Delivery mode**: `Online`

   **What's actually happening under the hood (worked example, kept for
   anyone debugging a mismatch):** Tehran is `UTC+03:30` year-round (no
   DST — Iran abolished it in 2022). 5:30 PM Tehran on August 1, 2026 is
   `17:30 − 03:30 = 14:00` UTC, i.e. the wire value
   `2026-08-01T14:00:00Z` — this is the value
   `frontend/src/shared/time/tehranTime.ts`'s `fromTehranInput` computes
   and what the backend actually stores; it's what you should expect to see
   if you inspect the request body or the database directly (Step 7 below).

   Submit. **Expect:** a success message with the new **Availability Slot
   id** and a copy button — copy it, call it `SLOT_ID`.

## 6. Log in as the Student and book the session

1. Log out, log in as `student@tutorflow.dev` / `Seed-Password-123!`
   (the seeded Student — no need to register a new one).
2. Navigate to **Book Session** (`/scheduling/sessions/book`).
   - **Availability Slot id**: `SLOT_ID`
   - **Student id**: you'll need the seeded Student's own id — find it via
     the Student's own detail page after logging in (the app shows the
     current account's id somewhere in the shell/profile area; if you
     can't locate it quickly, query it: see the `psql` query in Step 7
     below, adapted to `SELECT "Id" FROM "Students" WHERE "Email" =
     'student@tutorflow.dev';`).
   - **Parent/Guardian id**: leave blank (an adult Student can book
     independently, IDR-5).

   Submit. **Expect:** a success message with the new **Session id** — copy
   it, call it `SESSION_ID`.

## 7. Verify the booking directly in PostgreSQL

Confirms the write actually reached the real database, not just the UI's
own optimistic state. From any terminal with `psql` available:

```bash
psql -U postgres -h localhost -d tutorflow_dev -c "SELECT \"Id\", \"TutorId\", \"StudentId\", \"AvailabilitySlotId\", \"ScheduledTimeUtc\", \"Status\" FROM \"Sessions\" WHERE \"Id\" = 'SESSION_ID';"
```

(substitute your real `SESSION_ID` for the placeholder, keeping the single
quotes). **Expect:** exactly one row, `"TutorId"` matching `TUTOR_ID`,
`"AvailabilitySlotId"` matching `SLOT_ID`, `"ScheduledTimeUtc"` reading
`2026-08-01 14:00:00+00` (or your chosen time), and `"Status"` = `0`
(Scheduled — the four `SessionStatus` values are `0=Scheduled,
1=Completed, 2=Cancelled, 3=NoShow`, `backend/src/Domain/Scheduling/ValueObjects/SessionStatus.cs`).

Also worth confirming the slot itself is now consumed, the same check
Phase 2's `BookSession_persists_AvailabilitySlot_IsConsumed_against_real_Postgres`
test automates:

```bash
psql -U postgres -h localhost -d tutorflow_dev -c "SELECT \"IsConsumed\" FROM \"AvailabilitySlots\" WHERE \"Id\" = 'SLOT_ID';"
```

**Expect:** `t` (true).

## 8. Log out and back in — confirm the session is still there

1. Log out (as the Student).
2. Log back in as the same Student (`student@tutorflow.dev` /
   `Seed-Password-123!`).
3. Navigate to the session detail page
   (`/scheduling/sessions/SESSION_ID`).

**Expect:** the same session, same details, loaded fresh after a real
re-authentication — not from any client-side cache carried over from
booking it. This is the same "does it survive a real round trip, not just
an in-memory optimistic update" check as Step 4's Tutor-approval refresh,
this time for the whole booking flow.

---

**If every step above matched its "Expect," the full stack — frontend,
API, real PostgreSQL, the Phase 1B persistence fix, CORS
(`docs/phases/PHASE-025-REPORT.md` Task 1), and the Tehran-local time UX
(`docs/phases/PHASE-03-REPORT.md`) — works together end to end.**
