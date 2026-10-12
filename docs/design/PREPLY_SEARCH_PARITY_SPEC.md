# Preply Search Parity Spec

Supersedes the Search-specific portions of `PREPLY_UI_UX_AUDIT.md` (that pass could not reach
the results page). Researched via `WebFetch`/`WebSearch` against official Preply sources only
(preply.com, help.preply.com) — no proprietary text or images copied, structure/behavior
described in our own words. Every item below is marked **Confirmed** (with source),
**Partially confirmed** (marketing copy found, exact mechanics not verified), or
**Not confirmed**.

## Entry point / subject selection — Confirmed

Search starts from a subject-scoped landing URL, e.g. `preply.com/en/online/english-tutors`.
Source: `https://preply.com/en/online/english-tutors` (fetched directly).

## Filter controls — Confirmed

Fetched directly from the English-tutors search page:

| Filter | Notes |
|---|---|
| "I want to learn" | subject selection |
| Price per lesson | range, e.g. €2–€36+ |
| Country of birth | "Any country" default |
| "I'm available" | time/schedule selection — exact granularity (day-of-week grid vs. coarse "next 7 days") **not observable** from the fetched markup alone |
| Specialties | e.g. Business English, exam prep, accent reduction, kids (specific list not fully enumerable from the fetch) |
| "Also speaks" | other languages the tutor speaks |
| Native speaker | toggle |
| Tutor categories | Professional / Super Tutor designations, filterable |
| Sort tutors by | dropdown, default "Our top picks" |

## Professional Tutor badge — Confirmed

Source: `https://help.preply.com/en/articles/9795272-what-is-a-professional-tutor`.

Requires ONE of:
1. A passing grade in Preply's own "Online Language Teaching Course"
2. A government-accredited/endorsed teaching certificate from a course of ≥120 hours

Distinguishing trait vs. Super Tutor: **formal qualification**, assessed once (not a rolling window).

## Super Tutor badge — Confirmed

Sources: `https://help.preply.com/en/articles/6062191-what-is-a-super-tutor`,
`https://help.preply.com/en/articles/14880665-how-super-tutor-metrics-and-criteria-work`.

All 9 criteria, measured over the trailing 90 days, reassessed on the 1st of each month:

| Criterion | Threshold |
|---|---|
| Lessons taught | ≥ 40 |
| Average review rating | ≥ 4.8 |
| Trial→subscription conversion | ≥ 50% |
| Pre-trial message response | ≥ 80% within 24h |
| No-shows | 0 |
| Trial cancellation/reschedule rate | < 5% |
| Regular-lesson cancellation/reschedule rate | < 20% |
| Classroom usage for trials | ≥ 80% |
| Post-trial message response | ≥ 80% within 24h |

Badge is **revoked immediately** on any lesson no-show, independent of the monthly cycle.
Badge holders get a reported +15% trial-booking rate.

**Not confirmed**: the exact underlying data points (e.g. "message within 24h") are self-reported
by Preply's help article, not independently verifiable — treat the thresholds as directionally
correct, not byte-exact.

## Tutor result card — Confirmed (structure), Partially confirmed (exact field set)

Confirmed present (fetched directly from the search results page):
- Photo/avatar, name (clickable), country flag icon
- Professional/Super Tutor badge, when applicable
- Rating (e.g. "5") + review count (e.g. "30 reviews")
- Price per 50-minute lesson (e.g. "€45")
- Active student count, total lesson count
- Spoken languages (native + secondary, labeled distinctly)
- 2–3 short descriptor tags (e.g. "Engaging," "Encouraging")
- Short bio/headline, expandable "Learn more"
- Two CTAs: **"Book trial lesson"** and **"Send message"**

**Favorite/heart icon** — Confirmed via Preply's own help content (not the fetched card markup
directly): a heart icon on the card and on the tutor's own profile saves them to a
"Favourites"/"Save to my list" page. Source: help.preply.com search results (`how-to-get-into-
the-list-of-my-favourites`, `access-to-saved-tutors`).

## Sorting — Partially confirmed

Default is "Our top picks" (a relevance/ranking blend, not pure chronological or rating order).
Other sort options exist in the dropdown but their exact labels were **not individually
enumerable** from the fetched markup — price and rating sort are near-certain for a marketplace
of this shape but not literally confirmed by name.

## Pagination — Confirmed

Numbered pages + "next" arrow. URL pattern: `/en/online/english-tutors?page=[number]` — **not**
infinite scroll.

## URL/shareable search state — Partially confirmed

Page number is confirmed as a URL query parameter. Other filters (price, country, availability,
specialties) "appear to be" query parameters per the fetch, but the exact parameter names were
**not confirmed** — the fetched content was HTML-to-markdown converted, which lost the literal
query-string of filter-adjusted URLs (only the base/page-N URLs were directly visible).

## Mobile filter UX — Not confirmed

No mobile-specific rendering was reachable via `WebFetch` (it fetches desktop markup). Whether
Preply uses a bottom sheet, a full-screen filter page, or an inline collapsible panel on mobile
is **not confirmed** — TutorFlow's own `Drawer`-based approach (already built) is a reasonable,
independently-justified mobile pattern for this control density, not a verified copy of Preply's.

## Active filter chips / clear filters — Not confirmed

Not observable in the fetched markup (chips are typically client-rendered state, not present in
a server-fetched HTML snapshot). Not disconfirmed either — absence of evidence, not evidence of
absence. TutorFlow's existing `ActiveFilterChips` component is a reasonable independent design,
not a verified Preply copy.

## Empty state / no-results — Not confirmed

Could not be reached (requires an actual zero-result filter combination submitted interactively,
not reachable via a static fetch).

## What this means for TutorFlow

Confirmed-with-a-source items are safe to treat as real product requirements (badges, country of
birth, native speaker, specialties, price, availability, name/keyword via "I want to learn" being
subject-anchored, sort, pagination, favorite heart, two-CTA card, lesson/review/student counts).
Not-confirmed items (mobile mechanics, active-filter-chip behavior, empty-state copy, exact sort
option list) are **not** blocked on Preply parity — TutorFlow's own already-built equivalents
stand on their own UX merits, per the standing instruction not to copy mechanics we can't verify.
