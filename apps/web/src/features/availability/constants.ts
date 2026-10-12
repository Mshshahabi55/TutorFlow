/** Shared between the onboarding wizard's Availability step and the tutor-facing
 * availability-management page — the day-selection UI and the day-name↔number mapping needed
 * because the backend returns `DayOfWeek` as a string (`"Monday"`) but the weekly-rules PUT body
 * sends it as a number (matching `ReplaceWeeklyRules.Command`'s existing, unmodified contract). */
export const DAY_OPTIONS = [
  { value: '1', label: 'Monday' },
  { value: '2', label: 'Tuesday' },
  { value: '3', label: 'Wednesday' },
  { value: '4', label: 'Thursday' },
  { value: '5', label: 'Friday' },
  { value: '6', label: 'Saturday' },
  { value: '0', label: 'Sunday' },
];

const DAY_NAME_TO_NUMBER: Record<string, number> = {
  Sunday: 0,
  Monday: 1,
  Tuesday: 2,
  Wednesday: 3,
  Thursday: 4,
  Friday: 5,
  Saturday: 6,
};

export function dayNameToNumber(dayName: string): number {
  return DAY_NAME_TO_NUMBER[dayName] ?? 1;
}
