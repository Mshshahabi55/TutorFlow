/**
 * RC5.0: no backend capability exists for Learning Plans or Enrollment yet
 * — `docs/adr/ADR-021-learning-plans-enrollment-and-settlement-architecture.md`
 * is Proposed, not Accepted, and no entity, migration, or endpoint for
 * either concept exists anywhere in `backend/src`. This type exists only to
 * give the reusable `LearningPlanCard`/`LearningPlanSkeleton` components a
 * stable shape to render against once a real API exists — no page in this
 * app constructs a real value of this type today. Every real usage renders
 * an honest empty state instead (`docs/adr/ADR-021...`'s own Status note:
 * "does not itself authorize writing any of it").
 */
export type LearningPlanStatus = "Active" | "Draft" | "Archived";

export interface LearningPlanPreview {
  learningPlanId: string;
  title: string;
  /** 30 / 45 / 60 / 90, per the plan durations named in RC5.0's own brief — not a hardcoded enum, any positive number of days is a valid plan. */
  durationDays: number;
  sessionsPerWeek: number;
  totalSessions: number;
  /** Whole Rial, displayed in Toman via `shared/money/rial` — same convention every other price in this app already follows (ADR-019). */
  price: number;
  description: string;
  status?: LearningPlanStatus;
}
