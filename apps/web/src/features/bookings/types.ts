export type BookingStatus =
  | 'Held'
  | 'PendingPayment'
  | 'Confirmed'
  | 'Rescheduled'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow'
  | 'Expired'
  | 'Refunded';

/** Mirrors `CreateTrialHold.Response` (Bookings module) — the real commit moment (ADR-0027 §7).
 * `id` is the opaque booking identifier the frontend must carry forward but never surface to
 * the student as a concept they need to understand. */
export interface TrialHold {
  id: string;
  status: BookingStatus;
  startUtc: string;
  endUtc: string;
  holdExpiresAtUtc: string | null;
  priceAmountMinorUnits: number;
  priceCurrency: string;
}

/** Mirrors `CheckoutBooking.Response`. */
export interface CheckoutIntent {
  paymentReferenceId: string;
  clientSecret: string;
}

export interface CreateTrialHoldCommand {
  tutorUserId: string;
  durationMinutes: number;
  startUtc: string;
}
