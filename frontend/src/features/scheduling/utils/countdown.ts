/**
 * A plain elapsed-time countdown against the current instant — no Tehran
 * offset math involved (a duration is the same length regardless of
 * timezone), so this lives here rather than in `shared/time/tehranTime.ts`,
 * which owns only UTC<->Tehran conversions. Shared by the Tutor and Parent
 * Dashboards' own "Next Lesson" hero cards.
 */
export function formatCountdown(scheduledTimeUtc: string, now: number): string {
  const diffMinutes = Math.round((Date.parse(scheduledTimeUtc) - now) / 60_000);

  if (diffMinutes <= 0) {
    return "Starting now";
  }

  const hours = Math.floor(diffMinutes / 60);
  const minutes = diffMinutes % 60;

  if (hours === 0) {
    return `in ${minutes} min`;
  }
  if (minutes === 0) {
    return `in ${hours}h`;
  }
  return `in ${hours}h ${minutes}min`;
}
