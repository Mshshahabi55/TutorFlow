/**
 * The single owner of every UTC <-> Asia/Tehran conversion in this app
 * (PHASE-03). No component may do timezone math inline.
 *
 * Tehran is a fixed UTC+03:30 offset with no daylight saving (Iran
 * abolished DST in 2022 — ADR-018). Because the offset never changes, this
 * module uses plain millisecond arithmetic rather than the host's/ICU's
 * IANA timezone database: correctness here does not depend on the
 * runtime's tz-data being current, and it cannot accidentally inherit a
 * DST rule from any other zone.
 */

// Tehran's UTC offset, as of this writing: +03:30 (210 minutes), fixed,
// no DST. This is deliberately a hardcoded constant rather than a value
// resolved from `Intl`/the host's IANA timezone database, so that
// correctness here never depends on the runtime's tz-data being current —
// see the module comment above.
//
// This is an assumption with a real expiry condition, not a law of
// physics: Iran has changed this before (DST was abolished only in 2022)
// and could change its offset again. If that ever happens, **this
// constant is the only place in the codebase that needs to change** —
// every conversion in this module, and every caller across the app, reads
// through it.
const TEHRAN_OFFSET_MINUTES = 3 * 60 + 30;
const TEHRAN_OFFSET_MS = TEHRAN_OFFSET_MINUTES * 60_000;

/** HTML5 `<input type="datetime-local">` value shape: "YYYY-MM-DDTHH:mm" (seconds optional). */
const LOCAL_INPUT_PATTERN = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?$/;

function parseUtcIso(utcIsoString: string): number {
  const ms = Date.parse(utcIsoString);
  if (Number.isNaN(ms)) {
    throw new Error(`Invalid UTC ISO date/time: "${utcIsoString}"`);
  }
  return ms;
}

/** The UTC instant, shifted by the fixed Tehran offset, exposed only via UTC-getter reads below. */
function toTehranShifted(utcIsoString: string): Date {
  return new Date(parseUtcIso(utcIsoString) + TEHRAN_OFFSET_MS);
}

function pad2(value: number): string {
  return String(value).padStart(2, "0");
}

/**
 * True if `utcIsoString`, read as a Tehran-local calendar date, falls on
 * the same day as `now` (also read as Tehran-local; defaults to the
 * current instant). Presentational only — used to group a Tutor's
 * schedule into "Today" vs "Upcoming" without any new business rule.
 */
export function isTodayInTehran(utcIsoString: string, now: Date = new Date()): boolean {
  const target = toTehranShifted(utcIsoString);
  const reference = toTehranShifted(now.toISOString());
  return (
    target.getUTCFullYear() === reference.getUTCFullYear() &&
    target.getUTCMonth() === reference.getUTCMonth() &&
    target.getUTCDate() === reference.getUTCDate()
  );
}

/**
 * A UTC ISO 8601 instant -> a human-readable Tehran-local string, e.g.
 * "Aug 1, 2026, 17:30". Does not append a "(Tehran)" label — callers own
 * that, matching the existing "(UTC)" label-on-the-field-name convention.
 */
export function toTehranDisplay(utcIsoString: string): string {
  const shifted = toTehranShifted(utcIsoString);
  // Formatted with timeZone: "UTC" against an already-shifted instant, so
  // Intl only supplies locale-aware month/weekday names — it never
  // resolves an actual IANA zone, so it cannot reintroduce a DST rule.
  return new Intl.DateTimeFormat("en-US", {
    timeZone: "UTC",
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(shifted);
}

/**
 * A UTC ISO 8601 instant -> the value an `<input type="datetime-local">`
 * should be bound to for display in Tehran local time.
 */
export function toTehranInputValue(utcIsoString: string): string {
  const shifted = toTehranShifted(utcIsoString);
  return (
    `${shifted.getUTCFullYear()}-${pad2(shifted.getUTCMonth() + 1)}-${pad2(shifted.getUTCDate())}` +
    `T${pad2(shifted.getUTCHours())}:${pad2(shifted.getUTCMinutes())}`
  );
}

/**
 * A UTC ISO 8601 instant -> its Tehran-local calendar date, as a stable
 * "YYYY-MM-DD" grouping key (the date portion of `toTehranInputValue`).
 * Used by the booking wizard's "Choose Date" step to group a Tutor's open
 * Availability Slots by day without any new offset math.
 */
export function tehranDateKey(utcIsoString: string): string {
  return toTehranInputValue(utcIsoString).slice(0, 10);
}

/**
 * A `tehranDateKey` -> a human-readable label, e.g. "Sat, Aug 01". Formats
 * against `timeZone: "UTC"` the same way `toTehranDisplay` does — the key
 * already represents a Tehran-local calendar date, so this only supplies
 * locale-aware names, it never re-resolves an actual IANA zone.
 */
export function tehranDateLabel(dateKey: string): string {
  return new Intl.DateTimeFormat("en-US", {
    timeZone: "UTC",
    weekday: "short",
    month: "short",
    day: "2-digit",
  }).format(new Date(`${dateKey}T00:00:00Z`));
}

/** True if `value` is a syntactically and calendrically valid datetime-local string. */
export function isValidTehranLocalInput(value: string): boolean {
  const match = LOCAL_INPUT_PATTERN.exec(value);
  if (!match) {
    return false;
  }
  const [, year, month, day, hour, minute, second] = match;
  const ms = Date.UTC(
    Number(year),
    Number(month) - 1,
    Number(day),
    Number(hour),
    Number(minute),
    Number(second ?? "0"),
  );
  const asDate = new Date(ms);
  // Date.UTC silently rolls invalid fields (e.g. month 13, day 32) into the
  // next period instead of failing — reject anything that doesn't round-trip.
  return (
    asDate.getUTCFullYear() === Number(year) &&
    asDate.getUTCMonth() === Number(month) - 1 &&
    asDate.getUTCDate() === Number(day) &&
    asDate.getUTCHours() === Number(hour) &&
    asDate.getUTCMinutes() === Number(minute)
  );
}

/**
 * A Tehran-local `<input type="datetime-local">` value -> a UTC ISO 8601
 * string with an explicit "Z", exactly the wire format the API expects.
 */
export function fromTehranInput(localDateTime: string): string {
  const match = LOCAL_INPUT_PATTERN.exec(localDateTime);
  if (!match) {
    throw new Error(`Invalid Tehran local date/time: "${localDateTime}"`);
  }
  const [, year, month, day, hour, minute, second] = match;
  const asUtcMs = Date.UTC(
    Number(year),
    Number(month) - 1,
    Number(day),
    Number(hour),
    Number(minute),
    Number(second ?? "0"),
  );
  return new Date(asUtcMs - TEHRAN_OFFSET_MS).toISOString();
}
