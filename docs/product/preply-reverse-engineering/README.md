# Preply Reverse-Engineering — Durable Product Benchmark

```text
STATUS: PREPLY REVERSE-ENGINEERING DOCUMENTATION — IN PROGRESS

Public anonymous experience: PARTIALLY OBSERVED
Official documentation: DOCUMENTED
Visual screenshot evidence: AVAILABLE — 52 screenshots (tutor-signup wizard, homepage, "Find your
  tutor" matching quiz, authenticated search/profile/checkout, two real transactional emails;
  desktop only)
Authenticated experience: PARTIALLY DOCUMENTED / NOT FULLY BROWSER-VERIFIED
Full Preply parity: NOT YET VERIFIED
```

## 1. What this is

This directory is the durable, written record of every Preply reverse-engineering pass performed across this project's working sessions: the original full product-parity audit, the master-roadmap revalidation, the full user-journey reverse-engineering (Student/Tutor/Platform), the real-browser public-pages investigation, and every piece of official-source research that fed them. It consolidates that work into one navigable structure instead of leaving it scattered across conversation history.

## 2. Why it exists

Per the permanent TutorFlow governance rule:

```text
PREPLY = PRODUCT BENCHMARK
.NET CLAUDE KIT = ENGINEERING / ARCHITECTURE BENCHMARK
TUTORFLOW = THE IMPLEMENTATION
```

Every feature, wizard, page, and workflow in TutorFlow must be benchmarked against the **current, observable** Preply product before being considered complete — not against an older product brief, not against assumption, and not against TutorFlow's own prior implementation merely because it already exists. This directory is where that benchmark lives so future sessions don't have to re-derive it from scratch, and so it can be challenged, corrected, and extended as new evidence arrives.

## 3. Preply is a UX/behavior benchmark, not a source-code benchmark

TutorFlow replicates Preply's **observable product behavior** — journeys, information architecture, business rules, states, edge cases. It does **not** copy Preply's source code, proprietary assets, branding, or internal implementation, none of which is knowable from the outside and none of which was ever investigated here. Where Preply's internal technology is unknown (e.g., exactly how its own payment confirmation pipeline works), this documentation reproduces the *observable* behavior only, and TutorFlow implements it using its own, independently-designed architecture (the .NET Claude Kit).

## 4. Verification labels — use exactly these, never collapse them

| Label | Meaning |
|---|---|
| **OBSERVED** | Directly seen/interacted with on the live, public Preply website during a real browsing session. |
| **DOCUMENTED** | Supported by official Preply documentation (help.preply.com) or an official Preply page (e.g. the Become-a-Tutor marketing page), but not directly browser-observed in this project's sessions. |
| **PARTIALLY OBSERVED** | Some of the behavior was directly observed; the rest was not reachable (usually because it sits behind authentication). |
| **NOT VERIFIED** | Known or suspected, but no sufficient evidence — official or observed — was found. Never upgrade this to OBSERVED or DOCUMENTED without new evidence. |
| **BLOCKED** | Investigation stopped at this exact point because continuing required authentication, account creation, a real payment, or another real-world action this project deliberately does not take against Preply's live production system. |
| **INFERRED** | A reasonable inference only (e.g. "guest booking almost certainly shows a login modal, matching universal UX convention and TutorFlow's own existing design") — never presented as confirmed Preply behavior. |
| **INTENTIONAL DEVIATION** | A deliberate TutorFlow decision to differ from the Preply benchmark, with the reason recorded. |

A finding's label is permanent evidence of *how* it was established. **Never silently promote a label** (e.g. INFERRED → OBSERVED) just because it would make a later report look more complete.

## 5. The real-user / public-only boundary

One pass in this project's history performed genuine real-browser observation of Preply's live, public, **unauthenticated** pages only (homepage, tutor search, a real public tutor profile, the Help Center home, the Become-a-Tutor landing page — see `01-real-user-observations.md`). It deliberately did **not**:

- create a student account
- create a tutor account
- submit a tutor application
- make a real booking
- make a real payment
- make a real withdrawal
- message a real tutor or student
- file a real trust & safety report

Every authenticated or transactional journey in this documentation is therefore labeled **DOCUMENTED**, **NOT VERIFIED**, or **BLOCKED** — never OBSERVED — unless a specific fact happened to also be confirmable from a public page (e.g., commission rates were found on the public Become-a-Tutor marketing page, so they carry a DOCUMENTED label, not BLOCKED).

```text
REAL USER PUBLIC BROWSER OBSERVATION  ≠  FULL AUTHENTICATED PREPLY EXPERIENCE
```

Do not read any document in this directory as claiming the authenticated Preply product was fully browser-verified. It was not.

A separate evidence pack (13 screenshots, `evidence/tutor.zip`, see `18-screenshot-evidence.md`) was later supplied, showing a real tutor-signup session **filled in but never submitted** — "Complete registration" was not clicked, confirmed directly by the account holder. This stays within the same boundary (no tutor application was actually submitted to Preply) but reached one step further than the WebFetch-based pass above (an in-progress authenticated-adjacent wizard, not just public pages). The screenshots contain real PII (name, email, a face photo); that PII has been redacted from every document in this directory, including this one — see `18-screenshot-evidence.md`'s PII notice.

A third evidence pack (35 screenshots, `evidence/BeforFindYourTutor.zip` + `evidence/FindYourTutor.zip`) goes further still: it shows a real Google OAuth sign-in being completed into a **pre-existing real Preply student account** (evidenced by a real conversation dated weeks before the capture session), and reaching a real checkout page for a real trial lesson. **No payment was submitted** — the payment form's fields were left empty and the pay button stayed in its disabled state; the session navigated back to the tutor's profile instead of completing the purchase. This is a materially different category of evidence than the first two passes (a real authenticated session on an existing account, not just an anonymous or in-progress-signup one) and is flagged prominently, not just here, in `18-screenshot-evidence.md`'s own PII/evidence notice. All PII (names, emails from two different real Google accounts visible in the account chooser) is redacted throughout this directory.

A fourth evidence pack (4 screenshots, `evidence/PreplySentEmailForMe.zip`) shows two real emails Preply actually sent to the account holder's real Gmail inbox: a tutor-message notification, and an **abandoned-checkout recovery email** referencing the exact tutor/slot abandoned in the third pack. The account holder's real email address appears once, in an email-footer unsubscribe line, and is redacted. This is the first real evidence anywhere in this project of Preply's actual notification/email system.

## 6. Relationship to pre-existing `docs/product/` documents

Before this reverse-engineering work existed, `docs/product/` already contained planning documents (`PRODUCT_VISION.md`, `STUDENT_JOURNEY.md`, `TUTOR_JOURNEY.md`, `TRIAL_BOOKING_WORKFLOW.md`, `SUBSCRIPTION_JOURNEY.md`, `TUTOR_ONBOARDING_WIZARD_SPEC.md`, `STUDENT_DASHBOARD_SPEC.md`, `TUTOR_DASHBOARD_SPEC.md`, `NAVIGATION_AND_PAGE_MAP.md`, `ROLES_AND_PERMISSIONS.md`, `ADMIN_JOURNEY.md`) written from an originating product brief, **not** from verified Preply research. Those documents are **not modified or deleted** by this work. Where this reverse-engineering directly confirms, extends, or **conflicts** with something in that older material, the relevant document here says so explicitly and links to the older file — it does not silently overwrite it. The most significant known conflict is recorded in `07-wizard-inventory.md` and `16-product-decisions-and-open-questions.md`: the older `TUTOR_JOURNEY.md` specifies a "17-step wizard, as specified" from the original brief, while this session's actual Preply research found only **7 confirmed sections** in Preply's real, official tutor-registration flow, with an acknowledged 8th section Preply's own article doesn't identify. Both are preserved; neither is resolved here.

## 7. How future sessions should use these documents

1. Before implementing or revalidating any product feature, read the relevant document(s) here first — they are the current state of Preply-benchmark knowledge.
2. Trust the verification label over the prose. A DOCUMENTED or NOT VERIFIED claim is not the same confidence level as an OBSERVED one, even if both read as plain statements.
3. `IMPLEMENTED ≠ COMPLETE`. A TutorFlow feature matching a MATCHED row in `13-tutorflow-current-state-and-parity.md` still needs its own backend/frontend/test/architecture verification before being called done — this directory records product parity, not implementation completeness.
4. Never treat a gap in this documentation as permission to invent Preply behavior. An undocumented area is `NOT VERIFIED`, not a blank canvas.

## 8. How to update these documents

When new Preply research happens (official-source research, a further real-browser pass, or new authenticated findings if ever explicitly authorized):

1. Add the new evidence to `17-evidence-and-sources.md` first, with a new source ID.
2. Update the relevant topic document(s), citing the new source ID and the correct verification label.
3. If new evidence **upgrades** a label (e.g. NOT VERIFIED → DOCUMENTED because an official article was found), say so explicitly in the document rather than quietly changing the label with no trace.
4. If new evidence **conflicts** with existing material, record the conflict in `16-product-decisions-and-open-questions.md` — do not silently pick a winner.
5. Update `13-tutorflow-current-state-and-parity.md` only after re-checking the actual current TutorFlow code — never assume the matrix is still accurate without re-verifying the TutorFlow side.

## 9. Navigation Index

| Document | Contents |
|---|---|
| [00-scope-and-methodology.md](00-scope-and-methodology.md) | Governing rules, decision order, methodology, relationship to prior docs |
| [01-real-user-observations.md](01-real-user-observations.md) | The real public-browser session log |
| [02-student-journey.md](02-student-journey.md) | Complete Student journey, anonymous through authenticated |
| [03-tutor-journey.md](03-tutor-journey.md) | Complete Tutor journey, application through earnings |
| [04-platform-trust-and-safety.md](04-platform-trust-and-safety.md) | Moderation, blocking, suspension, verification, disputes |
| [05-navigation-and-page-inventory.md](05-navigation-and-page-inventory.md) | Navigation maps and full page inventory per role |
| [06-feature-inventory.md](06-feature-inventory.md) | Every discovered Preply capability |
| [07-wizard-inventory.md](07-wizard-inventory.md) | Tutor Application wizard — including the 7-vs-17-steps conflict |
| [08-state-machines.md](08-state-machines.md) | TutorApplication, Booking, Payment, Lesson, Review, Subscription, Account standing |
| [09-edge-cases.md](09-edge-cases.md) | Edge-case matrix by verification level |
| [10-mobile-responsive.md](10-mobile-responsive.md) | Desktop/mobile/app differences |
| [11-payments-subscriptions-and-economy.md](11-payments-subscriptions-and-economy.md) | Payment methods, commission, subscriptions, earnings, payouts |
| [12-classroom-messaging-reviews-notifications.md](12-classroom-messaging-reviews-notifications.md) | Classroom, Messaging, Reviews, Notifications |
| [13-tutorflow-current-state-and-parity.md](13-tutorflow-current-state-and-parity.md) | Master Preply ↔ TutorFlow parity matrix |
| [14-architecture-mapping.md](14-architecture-mapping.md) | Mapping product capabilities to the .NET Claude Kit architecture |
| [15-dependency-graph-and-roadmap.md](15-dependency-graph-and-roadmap.md) | Implementation dependency graph and P0–P3 roadmap |
| [16-product-decisions-and-open-questions.md](16-product-decisions-and-open-questions.md) | Every open/conflicting/pending decision discovered |
| [17-evidence-and-sources.md](17-evidence-and-sources.md) | Central source registry |
| [18-screenshot-evidence.md](18-screenshot-evidence.md) | Screenshot evidence inventory — real tutor-signup wizard session (13 images, desktop only) |
