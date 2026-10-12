'use client';

import { useMutation, useQuery } from '@tanstack/react-query';
import { checkoutBooking, createTrialHold, getBooking } from './api';

export function useCreateTrialHold() {
  return useMutation({ mutationFn: createTrialHold });
}

export function useCheckoutBooking() {
  return useMutation({ mutationFn: checkoutBooking });
}

/** Polls the real, server-authoritative booking status — the frontend never asserts "payment
 * succeeded" itself (ADR-0027 §7); only a webhook-driven Confirmed/Cancelled transition, read
 * back here, is trusted. */
export function useBooking(bookingId: string | null, pollForOutcome: boolean) {
  return useQuery({
    queryKey: ['bookings', bookingId] as const,
    queryFn: ({ signal }) => getBooking(bookingId!, signal),
    enabled: Boolean(bookingId),
    refetchInterval: (query) => {
      if (!pollForOutcome) return false;
      const status = query.state.data?.status;
      return status === 'PendingPayment' ? 2000 : false;
    },
  });
}
