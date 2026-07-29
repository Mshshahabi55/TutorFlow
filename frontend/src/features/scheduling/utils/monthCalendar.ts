import { tehranDateKey } from "@/shared/time/tehranTime";

/**
 * Pure month-grid date math, built entirely on `tehranDateKey` (the app's
 * one owner of UTC<->Tehran conversion — see `shared/time/tehranTime.ts`).
 * No new date/calendar library — every "YYYY-MM-DD"/"YYYY-MM" key is
 * treated as an already Tehran-local calendar date and manipulated with
 * plain `Date.UTC` arithmetic, the same technique `tehranDateLabel` itself
 * already uses to format a key without re-resolving a real timezone.
 */

export const WEEKDAY_LABELS = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"] as const;

function pad2(value: number): string {
  return String(value).padStart(2, "0");
}

function parseDateKey(dateKey: string): { year: number; month: number; day: number } {
  const [year, month, day] = dateKey.split("-").map(Number);
  return { year, month, day };
}

/** "YYYY-MM-DD" -> "YYYY-MM". */
export function monthKeyFromDateKey(dateKey: string): string {
  return dateKey.slice(0, 7);
}

/** Today's Tehran-local calendar date, as a "YYYY-MM-DD" key. */
export function todayDateKey(now: Date = new Date()): string {
  return tehranDateKey(now.toISOString());
}

/** The "YYYY-MM" month key containing today's Tehran-local calendar date. */
export function todayMonthKey(now: Date = new Date()): string {
  return monthKeyFromDateKey(todayDateKey(now));
}

/** `monthKey` shifted by `delta` calendar months (negative moves backward). */
export function addMonths(monthKey: string, delta: number): string {
  const { year, month } = parseDateKey(`${monthKey}-01`);
  const shifted = new Date(Date.UTC(year, month - 1 + delta, 1));
  return `${shifted.getUTCFullYear()}-${pad2(shifted.getUTCMonth() + 1)}`;
}

/** "YYYY-MM" -> a human-readable label, e.g. "August 2026". */
export function monthLabel(monthKey: string): string {
  const { year, month } = parseDateKey(`${monthKey}-01`);
  return new Intl.DateTimeFormat("en-US", { timeZone: "UTC", month: "long", year: "numeric" }).format(
    new Date(Date.UTC(year, month - 1, 1)),
  );
}

export interface MonthMatrixDay {
  dateKey: string;
  /** False for the leading/trailing days of the adjacent month shown to fill out a full week row. */
  isCurrentMonth: boolean;
}

/**
 * `monthKey` -> a full weeks×7 matrix (Sunday-first), including the
 * adjacent month's leading/trailing days needed to fill each row — the
 * same "always show complete weeks" convention every common calendar UI
 * uses, so a caller never has to special-case a short first/last row.
 */
export function buildMonthMatrix(monthKey: string): MonthMatrixDay[][] {
  const { year, month } = parseDateKey(`${monthKey}-01`);
  const firstWeekday = new Date(Date.UTC(year, month - 1, 1)).getUTCDay();
  const daysInMonth = new Date(Date.UTC(year, month, 0)).getUTCDate();
  const totalCells = Math.ceil((firstWeekday + daysInMonth) / 7) * 7;

  const cells: MonthMatrixDay[] = [];
  for (let i = 0; i < totalCells; i += 1) {
    const dayOffset = i - firstWeekday + 1;
    const cellDate = new Date(Date.UTC(year, month - 1, dayOffset));
    cells.push({
      dateKey: `${cellDate.getUTCFullYear()}-${pad2(cellDate.getUTCMonth() + 1)}-${pad2(cellDate.getUTCDate())}`,
      isCurrentMonth: cellDate.getUTCMonth() === month - 1 && cellDate.getUTCFullYear() === year,
    });
  }

  const weeks: MonthMatrixDay[][] = [];
  for (let i = 0; i < cells.length; i += 7) {
    weeks.push(cells.slice(i, i + 7));
  }
  return weeks;
}
