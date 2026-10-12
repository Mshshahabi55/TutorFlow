import { apiGet, apiPost } from '@/lib/api/client';
import { CheckoutIntent, CreateTrialHoldCommand, TrialHold } from './types';

export function createTrialHold(command: CreateTrialHoldCommand): Promise<TrialHold> {
  return apiPost<TrialHold, CreateTrialHoldCommand>('/api/bookings/trial-holds', command);
}

export function checkoutBooking(bookingId: string): Promise<CheckoutIntent> {
  return apiPost<CheckoutIntent>(`/api/bookings/${bookingId}/checkout`);
}

export function getBooking(bookingId: string, signal?: AbortSignal): Promise<TrialHold> {
  return apiGet<TrialHold>(`/api/bookings/${bookingId}`, undefined, { signal });
}
