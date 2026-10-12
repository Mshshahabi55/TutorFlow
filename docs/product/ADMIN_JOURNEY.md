# Admin Journey

Admin is a back-office surface, not a bounded context — see `docs/domain/DOMAIN_MODEL.md` and `docs/decisions/ADR-0008-admin-surface-composition.md` (Accepted) for why. "Composed Admin" is the adopted model: each business module owns its own admin-facing contracts; no 15th "Admin" bounded context exists.

## Primary admin workflows (from the page map)

| Workflow | Owning module (proposed) | Notes |
|---|---|---|
| Review Tutor Applications → Verify → Publish/Reject | Tutors | **IMPLEMENTED (ADR-0028)**: `/api/tutors/admin/applications` (list/detail) + begin-review/approve/reject/publish/suspend endpoints, `AdminOnly`-gated; minimal review-queue UI at `/admin/tutor-applications`. UI/UX is an engineering proposal (Preply's own admin tooling isn't publicly observable), not reverse-engineered parity |
| Suspend/reinstate accounts (Student or Tutor) | Identity (account status) + Tutors/Students (profile visibility) | **`[OPEN — Product Owner]`**: what triggers suspension — abuse reports, failed payments, manual-only? |
| Manage taxonomy: Subjects, Languages, Specialties | Subjects | Straightforward CRUD on reference data once Subjects exists |
| Search Management | Search | Likely: boost/demote rules, manual featured-tutor curation — exact scope **`[OPEN — Product Owner]`** |
| View/manage Bookings, Lessons | Bookings, Lessons | Support/investigation tooling, not a new workflow |
| Manage Subscriptions | Subscriptions | Support tooling (e.g. manual credit adjustment) — ledger adjustments need an explicit, audited admin action, not raw data edits |
| Payments: Refunds, Disputes, Payouts | Payments | High-risk surface — recommend restricting to a finance sub-role (see `ROLES_AND_PERMISSIONS.md` open question) |
| Reviews moderation | Reviews | Hide/remove abusive reviews; exact moderation policy **`[OPEN — Product Owner]`** |
| Messages (support visibility) | Messaging | **`[OPEN — Product Owner]`**: can admin read any student↔tutor conversation (support/dispute investigation), or only conversations explicitly escalated to support? This is a privacy-sensitive decision. |
| Support | Unclear — see `NAVIGATION_AND_PAGE_MAP.md` open question | Could be Messaging with an admin participant, or a separate ticketing concept not in the current 14 modules |
| Reports | Cross-cutting read models over multiple modules | Not a new bounded context; likely a reporting/analytics read layer — scope **`[OPEN — Product Owner]`** (what reports, what cadence) |
| Promotions | Not in the current 14 modules | **`[OPEN — Product Owner + Architect]`**: discount codes / referral credits touch Payments and Subscriptions pricing — needs its own design once pricing rules exist. Do not bolt onto Payments speculatively. |
| Notifications (admin view) | Notifications | Likely delivery-log/audit view, not authoring |
| Audit Logs | Cross-cutting | **`[OPEN — Architect]`**: centralized log (small module subscribing to all integration events) vs. per-module audit tables exposed through a common read contract. Recommend centralized subscriber — proposed in `ADR-0008`. |
| Feature Flags / System Settings | Platform/infrastructure, not a business module | Out of scope for the 14 bounded contexts; likely a lightweight ops concern (e.g. config service or even a hosted flag provider) — **`[OPEN — Architect]`** |

## Why this isn't designed further here

Several admin surfaces (Promotions, Support, Reports, Feature Flags) don't map cleanly onto any of the 14 existing modules and are explicitly flagged rather than assigned, per the instruction not to invent product/architecture decisions. Building admin tooling for modules that don't have their first real feature yet (e.g. a Payments admin refund screen before Payments has any payments) is also out of order — admin tooling for a given module should land after that module's core workflow exists, not before.
