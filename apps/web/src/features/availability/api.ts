import { apiGet, apiPut } from '@/lib/api/client';
import { AvailabilityConfiguration, BookingSettingsInput, WeeklyRuleInput } from './types';

/** Calls the existing self-service `Availability` endpoints directly — reused, not duplicated,
 * by the onboarding wizard's Availability step and the tutor-facing availability-management page
 * (`/become-a-tutor/availability`). Promoted here from `features/tutor-onboarding/` once a
 * second real consumer existed, exactly as ADR-0026 §5 anticipated. */

export function getCurrentAvailabilityConfiguration(signal?: AbortSignal): Promise<AvailabilityConfiguration> {
  return apiGet<AvailabilityConfiguration>('/api/availability/me', undefined, { signal });
}

export function replaceWeeklyRules(rules: WeeklyRuleInput[]): Promise<unknown> {
  return apiPut('/api/availability/me/weekly-rules', { rules });
}

export function updateBookingSettings(settings: BookingSettingsInput): Promise<unknown> {
  return apiPut('/api/availability/me/booking-settings', settings);
}
