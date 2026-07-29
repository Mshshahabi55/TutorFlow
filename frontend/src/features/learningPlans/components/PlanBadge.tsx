import { StatusPill, type StatusTone } from "@/shared/components/feedback/StatusPill";
import type { LearningPlanStatus } from "@/features/learningPlans/types";

const STATUS_TONE: Record<LearningPlanStatus, StatusTone> = {
  Active: "success",
  Draft: "neutral",
  Archived: "warning",
};

export interface PlanBadgeProps {
  /** Either a known plan lifecycle status (tone chosen automatically) or any other label (e.g. "30 Days") with an explicit tone. */
  label: string;
  tone?: StatusTone;
}

/**
 * The one badge used everywhere a Learning Plan's duration or lifecycle
 * status is shown — a thin, plan-specific adapter over the same `StatusPill`
 * every other status indicator in this app already uses (Session status,
 * Relationship status), not a second, parallel badge visual language.
 */
export function PlanBadge({ label, tone }: PlanBadgeProps) {
  const resolvedTone = tone ?? STATUS_TONE[label as LearningPlanStatus] ?? "info";
  return <StatusPill label={label} tone={resolvedTone} />;
}
