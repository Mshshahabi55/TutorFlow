export interface WeeklyRuleInput {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export interface BookingSettingsInput {
  timeZoneId: string;
  minNoticeHours: number;
  maxBookingWindowDays: number;
  defaultSlotDurationMinutes: number;
}

/** Mirrors one row of `GetCurrentConfiguration.WeeklyRuleResponse` (Availability module).
 * `dayOfWeek` arrives as a day-name string (e.g. `"Monday"`), not a number — the Host-wide
 * `JsonStringEnumConverter` (`TutorFlow.Host/Program.cs`) serializes every backend enum that way,
 * including `System.DayOfWeek`. `startTime`/`endTime` arrive as `"HH:mm:ss"` (`TimeOnly`'s
 * default JSON format). */
export interface WeeklyRule {
  id: string;
  dayOfWeek: string;
  startTime: string;
  endTime: string;
}

/** Mirrors `GetCurrentConfiguration.BookingSettingsResponse`. */
export interface BookingSettings {
  timeZoneId: string;
  minNoticeHours: number;
  maxBookingWindowDays: number;
  defaultSlotDurationMinutes: number;
  isConfigured: boolean;
}

/** Mirrors `GetCurrentConfiguration.Response` — the single source of truth for "what does this
 * tutor's availability currently look like," reused by both the onboarding wizard's Availability
 * step and the tutor-facing availability-management page (ADR-0026 §5, promoted to this shared
 * module now that the second real consumer it anticipated actually exists). */
export interface AvailabilityConfiguration {
  tutorUserId: string;
  weeklyRules: WeeklyRule[];
  bookingSettings: BookingSettings;
  isBookable: boolean;
}
