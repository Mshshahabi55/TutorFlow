# Scope and Methodology

## Governing rule

```text
CURRENT PREPLY PRODUCT
  ↓
PRODUCT SPECIFICATION
  ↓
TUTORFLOW GAP ANALYSIS
  ↓
.NET CLAUDE KIT ARCHITECTURE
  ↓
IMPLEMENTATION
  ↓
TESTING
  ↓
PREPLY PARITY VERIFICATION
```

This order is permanent and non-negotiable for this project. A feature is never designed by starting from a convenient library or an existing TutorFlow implementation and inventing product behavior around it — the current, observable Preply product is researched and understood first.

## Decision authority order

**Product behavior:**
1. Current verified Preply behavior (OBSERVED or DOCUMENTED)
2. Explicit Product Owner decision
3. Existing approved TutorFlow product specification / ADR
4. Current TutorFlow implementation
5. Assumption (lowest priority — avoid)

**Architecture:**
1. .NET Claude Kit established architecture/patterns
2. Approved TutorFlow ADRs
3. Existing TutorFlow implementation
4. Proposed design
5. Assumption (lowest priority — avoid)

## Methodology actually used across this project's reverse-engineering passes

1. **Documentation research** — WebSearch to locate the correct official `help.preply.com` article, WebFetch to read it directly. Preferred over third-party blogs/summaries whenever an official article exists. Every fact traces to a source ID in `17-evidence-and-sources.md`.
2. **Real public-browser observation** — WebFetch against the live, unauthenticated `preply.com` pages themselves (homepage, search results, a real public tutor profile, Help Center home, Become-a-Tutor landing). This is genuine page-level observation, not a documentation summary — but it is **not** full interactive browser automation: there is no persistent session, no literal click simulation, and no ability to see what happens after a button click (e.g., the exact login-modal behavior on "Book trial lesson" was never directly observed — it is INFERRED).
3. **Parallel research forks** — for broad research passes, multiple sub-agents researched disjoint topic areas in parallel against official sources, each reporting back with source/observation/confidence per fact, then synthesized by the orchestrating session into the documents here.
4. **TutorFlow code inspection** — direct reading of the actual current repository (never trusting a prior report's claim about the code without re-reading it), used to populate the parity matrix (`13-tutorflow-current-state-and-parity.md`) and architecture mapping (`14-architecture-mapping.md`).

## What was explicitly never done, and why

- **No Preply account was created** (student or tutor) — creating an account on a live commercial platform under automated/test pretenses raises real Terms-of-Service and real-world-consequence concerns (a fake tutor application would consume an actual human reviewer's time within Preply's own stated SLA). This boundary was set explicitly by the Product Owner for this project.
- **No real payment was attempted** — would be a genuine financial transaction on a third party's live system.
- **No message was sent to a real tutor or student** — would involve a real person under false pretenses.
- **No browser automation tool with session/click capability was used** — only available tooling was a static page-fetch tool (WebFetch); genuine click-driven interaction (e.g., observing the exact modal shown when a guest clicks "Book trial lesson") was out of reach and is labeled accordingly (INFERRED or BLOCKED), never faked as OBSERVED.

## Relationship to pre-existing `docs/product/*.md`

Those documents were written earlier in this project, from an originating product brief, before any of the Preply reverse-engineering recorded in this directory took place. They are **planning specifications**, not verified Preply research — many of their claims are explicitly marked `[OPEN — Product Owner]` or `[OPEN — Architect]` in their own text, and at least one (`TUTOR_JOURNEY.md`'s 17-step onboarding wizard) is now known to conflict with what real Preply research actually found (`07-wizard-inventory.md`). This directory does not edit or delete those files. Where this directory's findings bear on something those files assumed, the relevant document here says so explicitly and cross-references the older file by name.

## Scope boundary for this specific documentation-persistence task

This directory was created by a documentation-only task. No source code, test, migration, or architecture file was modified to produce it. Every file under `docs/product/preply-reverse-engineering/` is new; no pre-existing file elsewhere in the repository was changed.
