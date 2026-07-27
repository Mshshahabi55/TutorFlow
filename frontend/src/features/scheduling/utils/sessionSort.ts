import type { SessionDto } from "@/services/api/dtos";

/** Shared by the Tutor Workspace's own session groupings (Dashboard, My Sessions) — presentational ordering only, no business rule. */
export function byScheduledTimeAscending(a: SessionDto, b: SessionDto): number {
  return Date.parse(a.scheduledTimeUtc) - Date.parse(b.scheduledTimeUtc);
}

export function byScheduledTimeDescending(a: SessionDto, b: SessionDto): number {
  return Date.parse(b.scheduledTimeUtc) - Date.parse(a.scheduledTimeUtc);
}
