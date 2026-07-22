/**
 * The single owner of every Rial <-> Toman conversion and money format in
 * this app (PHASE-04 / ADR-019). No component may do the x10 inline.
 *
 * v1 prices exclusively in Iranian Rial (IRR), stored and transmitted as
 * whole numbers — Rial has no practical minor unit. Toman (1 Toman = 10
 * Rial) is what users actually think and enter in; it exists only at this
 * presentation seam, mirroring frontend/src/shared/time/tehranTime.ts's
 * UTC/Tehran split (Phase 3).
 */

const RIAL_PER_TOMAN = 10;

/**
 * Matches HourlyRate.MaxAmount exactly
 * (backend/src/Domain/Identity/ValueObjects/HourlyRate.cs) — the largest
 * multiple of 10 within numeric(12,0)'s ceiling (ADR-019 Addendum 1 /
 * Phase 4.5: the true schema ceiling of 999,999,999,999 is itself not a
 * legal HourlyRate, since it isn't divisible by 10).
 */
export const MAX_RIAL = 999_999_999_990;

/** The Toman value of MAX_RIAL — exact, since MAX_RIAL is now always divisible by 10. */
export const MAX_TOMAN = MAX_RIAL / RIAL_PER_TOMAN;

/**
 * A whole Toman amount (as entered by a user) -> the Rial value sent over
 * the wire. Always exact: Toman -> Rial is x10 on integers, never floating
 * point division, so this can never itself produce a fractional result.
 */
export function tomanToRial(toman: number): number {
  if (!Number.isInteger(toman) || toman < 0 || toman > MAX_TOMAN) {
    throw new Error(`Invalid Toman amount: ${toman}`);
  }
  return toman * RIAL_PER_TOMAN;
}

/**
 * A Rial amount (from the wire) -> the whole Toman value to display.
 *
 * STRICT — throws (does not round) if `rial` is not evenly divisible by
 * 10, a programmer-error guard, not a UI-facing function: since
 * `HourlyRate.Of` now enforces divisibility by 10 in Domain (ADR-019
 * Addendum 1 / Phase 4.5), no conforming write can ever produce a
 * non-divisible Rial amount, so a caller receiving one here has a real
 * bug to fix, not a value to gracefully paper over. Used for round-trip
 * correctness (tests, `tomanToRial`'s own inverse) — **no page render
 * path may call this directly**; use `formatToman`/`toTomanInputValue`
 * instead, which are safe for any value this system has ever persisted,
 * including data written before this invariant existed.
 */
export function rialToToman(rial: number): number {
  if (!Number.isInteger(rial) || rial < 0 || rial > MAX_RIAL) {
    throw new Error(`Invalid Rial amount: ${rial}`);
  }
  if (rial % RIAL_PER_TOMAN !== 0) {
    throw new Error(
      `Rial amount ${rial} is not evenly divisible by ${RIAL_PER_TOMAN} and cannot be ` +
        "expressed as a whole Toman value.",
    );
  }
  return rial / RIAL_PER_TOMAN;
}

/**
 * A Rial amount -> the whole Toman value to render, safe for any page
 * render path: rounds to the nearest Toman instead of throwing if `rial`
 * isn't evenly divisible by 10, rather than crashing on legacy data
 * written before Domain enforced the whole-Toman invariant (ADR-019
 * Addendum 1). The stored Rial amount is never altered by this — only the
 * displayed/edited Toman figure is rounded, and the moment that Tutor's
 * rate is next saved through this UI, it becomes an exact multiple of 10
 * again.
 */
function rialToTomanForDisplay(rial: number): number {
  if (!Number.isInteger(rial) || rial < 0 || rial > MAX_RIAL) {
    throw new Error(`Invalid Rial amount: ${rial}`);
  }
  return Math.round(rial / RIAL_PER_TOMAN);
}

/**
 * A Rial amount -> a thousand-separated Toman string for display, e.g.
 * `formatToman(500_000)` -> `"50,000"`. Does not append a "Toman" label —
 * callers own that, matching `toTehranDisplay`'s "no (Tehran) suffix"
 * convention. Safe for any render path — see `rialToTomanForDisplay`.
 */
export function formatToman(rial: number): string {
  return new Intl.NumberFormat("en-US").format(rialToTomanForDisplay(rial));
}

/**
 * A Rial amount -> the string an editable Toman `<input>` should be bound
 * to (e.g. the Tutor Offering form's default value). Safe for any render
 * path — see `rialToTomanForDisplay`.
 */
export function toTomanInputValue(rial: number): string {
  return String(rialToTomanForDisplay(rial));
}

/** True if `value` is a positive whole Toman amount within the schema's maximum. */
export function isValidTomanAmount(value: string): boolean {
  if (!/^\d+$/.test(value.trim())) {
    return false;
  }
  const parsed = Number(value.trim());
  return Number.isInteger(parsed) && parsed > 0 && parsed <= MAX_TOMAN;
}
