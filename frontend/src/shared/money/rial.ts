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
 * (backend/src/Domain/Identity/ValueObjects/HourlyRate.cs) — numeric(12,0)'s
 * ceiling, twelve nines.
 */
export const MAX_RIAL = 999_999_999_999;

/** The largest whole-Toman amount that converts to a Rial value within MAX_RIAL. */
export const MAX_TOMAN = Math.floor(MAX_RIAL / RIAL_PER_TOMAN);

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
 * Throws rather than rounding if `rial` is not evenly divisible by 10
 * (PHASE-04-REPORT.md Section 4: the phase's own absolute rule is to
 * report such a path, not silently choose a rounding rule). The only way
 * this can happen in practice is a raw API write bypassing the Toman-only
 * UI this module backs — every write this product itself makes goes
 * through `tomanToRial`, which always produces a multiple of 10.
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
 * A Rial amount -> a thousand-separated Toman string for display, e.g.
 * `formatToman(500_000)` -> `"50,000"`. Does not append a "Toman" label —
 * callers own that, matching `toTehranDisplay`'s "no (Tehran) suffix"
 * convention.
 */
export function formatToman(rial: number): string {
  return new Intl.NumberFormat("en-US").format(rialToToman(rial));
}

/** True if `value` is a positive whole Toman amount within the schema's maximum. */
export function isValidTomanAmount(value: string): boolean {
  if (!/^\d+$/.test(value.trim())) {
    return false;
  }
  const parsed = Number(value.trim());
  return Number.isInteger(parsed) && parsed > 0 && parsed <= MAX_TOMAN;
}
