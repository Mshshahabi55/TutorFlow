/** Splits a comma-separated free-text field into a trimmed, non-empty string list — same convention `TutorOfferingForm`'s "Offered durations (minutes, comma-separated)" field already established, generalized to plain strings. */
export function parseCommaList(value: string): string[] {
  return value
    .split(",")
    .map((entry) => entry.trim())
    .filter((entry) => entry.length > 0);
}

/** The inverse of parseCommaList — for prefilling a form field from an already-saved string array. */
export function formatCommaList(values: string[]): string {
  return values.join(", ");
}
