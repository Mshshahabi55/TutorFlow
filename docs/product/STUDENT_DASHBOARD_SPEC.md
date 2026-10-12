# Student Dashboard — Page Specification

**Status:** Specified. Not implemented.

**Route:** `/student/dashboard` (Student role only). Matches the "Home / Dashboard" row already named in `NAVIGATION_AND_PAGE_MAP.md`; this spec fills in the detail that document deferred.

No new backend module. Every section is composed at the frontend/BFF layer from existing modules.

## Sections

| Section | Priority | Data source (module) | Primary action(s) | States |
|---|---|---|---|---|
| Next lesson CTA | P0 | `Lessons` (soonest upcoming) + `Bookings` | Join classroom (if within join window, `ADR-0006`), view/reschedule/cancel | Empty: "no upcoming lessons" + CTA into Find Tutors. Loading: skeleton. "Join" disabled until the join window opens |
| Upcoming lessons (list) | P0 | `Lessons` + `Bookings` | View detail, reschedule, cancel | Same empty/loading as above |
| Continue learning | P1 | `Learning` (in-progress materials/homework) | Open material | Empty: hidden entirely if the student has no `Learning` activity yet (new student) |
| Tutor relationship (current tutor(s)) | P1 | `Bookings` (distinct tutors this student has booked) composed with `Tutors` (display data) | Message, book again | Empty: hidden if no tutor yet |
| Recent messages | P1 | `Messaging` (unread count + recent conversations) | Open conversation | Empty: "no messages yet" |
| Favorite tutors | P2 | `Students` (`FavoriteTutor`) composed with `Tutors` (display data) | View profile, book | Empty: "no favorites yet" + CTA into Find Tutors |
| Subscription state | P1 | `Subscriptions` | Manage subscription, view credits | Hidden entirely if the student has no active/past subscription (not everyone subscribes — this is not a universal section) |
| Recommended tutors | P2 | `Search` (a ranked query, e.g. same-subject-as-last-booking) | View profile | Empty: generic "popular tutors" fallback rather than a blank section |
| Empty state (brand-new student, no bookings at all) | P0 | — | A single unified "find your first tutor" CTA replaces most sections above rather than showing each one empty individually — avoids a dashboard that's mostly empty boxes on day one | — |

## What this spec does not decide

- "Recommended tutors" ranking logic — depends on `SEARCH_SPEC.md`'s relevance model, itself not fully tuned (`SEARCH_SPEC.md` §8).
- Visual layout/widget design — `docs/design/` territory.
