/**
 * The backend accepts a duration (a Tutor's offered session length, an
 * Availability Slot's length) as a .NET TimeSpan, serialized as an
 * "hh:mm:ss" string, and enforces only that it be positive (Domain:
 * Tutor.SetOfferedDurations, SessionDuration.Of). This module restricts the
 * UI to whole-minute precision — a deliberate simplification, not a
 * business rule the backend requires — since no approved document
 * specifies a required granularity. Promoted here from features/identity
 * once features/scheduling needed the same conversion (Sprint 7).
 */

export function minutesToTimeSpan(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  return `${String(hours).padStart(2, "0")}:${String(remainingMinutes).padStart(2, "0")}:00`;
}

export function timeSpanToMinutes(value: string): number {
  const [hours = 0, minutes = 0] = value.split(":").map(Number);
  return hours * 60 + minutes;
}

/** Parses a comma-separated "30, 60, 90" string into whole minutes. Assumes the caller has already validated the format. */
export function parseMinutesList(value: string): number[] {
  return value
    .split(",")
    .map((part) => part.trim())
    .filter((part) => part.length > 0)
    .map(Number);
}

export function formatMinutesList(durations: string[]): string {
  return durations.map(timeSpanToMinutes).join(", ");
}
