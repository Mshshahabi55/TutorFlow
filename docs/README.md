# TutorFlow Documentation

This tree is the product/architecture specification for TutorFlow, written before any business feature is implemented. It exists so the Product Owner and Architect can review and approve scope, workflows, and open decisions before `/scaffold` touches a single module.

## Conventions

- **Everything here is a proposal until explicitly approved.** Nothing in this tree authorizes implementation by itself.
- **`[OPEN — Product Owner]`** marks a product/business/legal decision this role is not authorized to make (pricing, cancellation policy, tax handling, legal copy, exact UX wording, etc.).
- **`[OPEN — Architect]`** marks a technical/architecture decision that affects module boundaries, data ownership, or cross-cutting infrastructure (e.g. which module owns the classroom, provider choices, repo topology).
- **ADRs (`docs/decisions/`) follow MADR-style status**: `Proposed` (options laid out, no decision made, must not influence code yet) or `Accepted` (already reflected in the current architecture / `CLAUDE.md`). An ADR being `Proposed` is not permission to implement any option it lists.
- When this documentation is updated, update the existing file — don't fork a second copy of the same content.

## Structure

| Path | Contents |
|---|---|
| `docs/product/PRODUCT_VISION.md` | Vision, target market, personas |
| `docs/product/ROLES_AND_PERMISSIONS.md` | Roles, permission boundaries per surface |
| `docs/product/NAVIGATION_AND_PAGE_MAP.md` | Page inventory and navigation structure for Public/Student/Tutor/Admin |
| `docs/product/STUDENT_JOURNEY.md` | Student-side end-to-end journey |
| `docs/product/TUTOR_JOURNEY.md` | Tutor-side journey incl. onboarding wizard |
| `docs/product/ADMIN_JOURNEY.md` | Admin journey and surfaces |
| `docs/product/TRIAL_BOOKING_WORKFLOW.md` | Trial booking as a transactional workflow (holds, payment, webhooks, concurrency) |
| `docs/product/SUBSCRIPTION_JOURNEY.md` | Subscription journey and ledger-based credit model |
| `docs/domain/DOMAIN_MODEL.md` | The 14 modules: responsibilities, boundaries, communication, shared kernel contents, known gaps |
| `docs/domain/STATE_MACHINES.md` | Proposed Booking / Payment / Subscription / Lesson state machines |
| `docs/design/DESIGN_SYSTEM_DIRECTION.md` | Design system token categories and component inventory (direction, not literal values) |
| `docs/design/UX_STATES_PRINCIPLES.md` | Loading/empty/error/validation/success/disabled/permission state policy |
| `docs/decisions/` | ADRs — one decision or decision-in-waiting per file |
| `docs/OPEN_QUESTIONS.md` | Master list of every unresolved decision across all of the above, with owner and blocking impact |

## Reading order for reviewers

1. `product/PRODUCT_VISION.md` + `ROLES_AND_PERMISSIONS.md` — what and for whom
2. `domain/DOMAIN_MODEL.md` — the 14 modules and their contracts
3. `product/NAVIGATION_AND_PAGE_MAP.md` + the three journey docs — the product surface
4. `product/TRIAL_BOOKING_WORKFLOW.md` + `domain/STATE_MACHINES.md` — the hardest, highest-risk workflow
5. `product/SUBSCRIPTION_JOURNEY.md` — second-hardest workflow
6. `design/*` — UI direction
7. `decisions/*` and `OPEN_QUESTIONS.md` — everything that still needs a decision before it can be built
