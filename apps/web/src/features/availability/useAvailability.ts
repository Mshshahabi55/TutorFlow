'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getCurrentAvailabilityConfiguration, replaceWeeklyRules, updateBookingSettings } from './api';
import { BookingSettingsInput, WeeklyRuleInput } from './types';

export const availabilityConfigurationQueryKey = ['availability', 'me'] as const;

export function useCurrentAvailabilityConfiguration(enabled: boolean) {
  return useQuery({
    queryKey: availabilityConfigurationQueryKey,
    queryFn: ({ signal }) => getCurrentAvailabilityConfiguration(signal),
    enabled,
  });
}

export function useReplaceWeeklyRules() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (rules: WeeklyRuleInput[]) => replaceWeeklyRules(rules),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: availabilityConfigurationQueryKey }),
  });
}

export function useUpdateBookingSettings() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (settings: BookingSettingsInput) => updateBookingSettings(settings),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: availabilityConfigurationQueryKey }),
  });
}
