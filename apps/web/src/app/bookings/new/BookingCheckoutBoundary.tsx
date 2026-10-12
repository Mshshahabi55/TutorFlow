'use client';

import { useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { PageContainer } from '@/components/layout/PageContainer';
import { Card } from '@/components/ui/Card';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { AuthGateModal } from '@/components/auth/AuthGateModal';
import { useAuth } from '@/features/auth/useAuth';
import { ApiError } from '@/lib/api/client';
import { SlotSelectionSummary } from '@/features/tutor-profile/components/SlotSelectionSummary';
import { useBooking, useCheckoutBooking, useCreateTrialHold } from '@/features/bookings/useBookingCheckout';
import { TrialPaymentForm } from './TrialPaymentForm';
import styles from './BookingCheckoutBoundary.module.css';

// Same local-formatter convention already used by BookingPanel.tsx/TutorResultCard.tsx — not
// extracted to a shared util, matching this codebase's existing precedent of independent copies.
function formatPrice(minorUnits: number, currency: string): string {
  return new Intl.NumberFormat('en-IE', { style: 'currency', currency, maximumFractionDigits: 2 }).format(
    minorUnits / 100,
  );
}

type BookingStatusCopy = { variant: 'success' | 'warning' | 'error' | 'info'; title: string; description: string };

// ADR-0027 §7: confirmation is reached only by polling GetBooking until the webhook-driven
// status lands — never asserted by the frontend. Once it does land, the terminal states deserve
// honest, human-friendly copy instead of a raw enum value (e.g. "Expired").
function bookingStatusCopy(status: string): BookingStatusCopy | null {
  switch (status) {
    case 'Confirmed':
      return {
        variant: 'success',
        title: 'Your trial lesson is confirmed',
        description: "You'll find it on your upcoming lessons.",
      };
    case 'Expired':
      return {
        variant: 'warning',
        title: 'Your hold expired',
        description: 'Payment wasn’t completed in time, so this time was released. Please reserve it again.',
      };
    case 'Cancelled':
      return {
        variant: 'error',
        title: 'This booking was cancelled',
        description: 'No charge was made. You can reserve a new time whenever you’re ready.',
      };
    case 'PendingPayment':
      return {
        variant: 'info',
        title: 'Waiting for payment confirmation',
        description: 'This updates automatically once your payment finishes processing.',
      };
    default:
      return null;
  }
}

function errorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    // A slot that disappeared between browsing and committing, or a hold that expired before
    // checkout, both surface as a 409 from the backend — never a raw technical error (§13).
    if (error.status === 409) {
      return "This time is no longer available. Please choose another time.";
    }
    return error.problem.detail ?? fallback;
  }
  return fallback;
}

/**
 * The real commit boundary (ADR-0027 §7) — browsing a slot never holds it; clicking "Reserve
 * this time" here is the first action that calls `CreateTrialHold` for real. Checkout then
 * calls the real `Payments` boundary; this phase stops exactly at the point a real payment
 * method would be collected — no fake "mark as paid" control exists anywhere in this component.
 */
export function BookingCheckoutBoundary() {
  // Read per-render rather than cached at module scope — a build sets this once and it never
  // changes at runtime, so there's no behavioral difference, but it keeps the "configured vs
  // not" branch below trivially testable without relying on module-import-time env snapshots.
  const stripePublishableKey = process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY;

  const searchParams = useSearchParams();
  const tutorUserId = searchParams.get('tutorUserId');
  const tutorSlug = searchParams.get('tutorSlug');
  const durationMinutes = searchParams.get('durationMinutes');
  const startUtc = searchParams.get('startUtc');
  const endUtc = searchParams.get('endUtc');
  const hasSelection = Boolean(tutorUserId && tutorSlug && durationMinutes && startUtc && endUtc);

  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const [authGateOpen, setAuthGateOpen] = useState(false);
  const [bookingId, setBookingId] = useState<string | null>(null);
  const [clientSecret, setClientSecret] = useState<string | null>(null);

  const createHold = useCreateTrialHold();
  const checkout = useCheckoutBooking();
  const booking = useBooking(bookingId, Boolean(clientSecret));

  const handleReserve = () => {
    if (!isAuthenticated) {
      setAuthGateOpen(true);
      return;
    }

    createHold.mutate(
      { tutorUserId: tutorUserId!, durationMinutes: Number(durationMinutes), startUtc: startUtc! },
      {
        onSuccess: (hold) => {
          setBookingId(hold.id);
          checkout.mutate(hold.id, { onSuccess: (intent) => setClientSecret(intent.clientSecret) });
        },
      },
    );
  };

  if (!hasSelection) {
    return (
      <PageContainer className={styles.page}>
        <Card className={styles.card}>
          <h1 className={styles.title}>No slot was selected</h1>
          <ButtonLink href="/find-tutors">Back to Find Tutors</ButtonLink>
        </Card>
      </PageContainer>
    );
  }

  return (
    <PageContainer className={styles.page}>
      <Card className={styles.card}>
        <h1 className={styles.title}>Confirm your trial lesson</h1>

        <div className={styles.selection}>
          <SlotSelectionSummary startUtc={startUtc!} endUtc={endUtc!} />
          <p className={styles.duration}>{durationMinutes} minute trial lesson</p>
          {createHold.data && (
            <p className={styles.duration}>
              Total: {formatPrice(createHold.data.priceAmountMinorUnits, createHold.data.priceCurrency)}
            </p>
          )}
        </div>

        {!bookingId && (
          <>
            <p className={styles.description}>
              Selecting a time doesn&apos;t reserve it — clicking below does, for a limited time while you complete
              checkout.
            </p>
            {createHold.isError && (
              <Alert variant="error" title="Couldn't reserve this time">
                {errorMessage(createHold.error, "Couldn't reserve this time. Please try again.")}
              </Alert>
            )}
            <Button onClick={handleReserve} loading={createHold.isPending} disabled={authLoading}>
              Reserve this time
            </Button>
          </>
        )}

        {bookingId && checkout.isPending && <p className={styles.description}>Starting checkout…</p>}

        {bookingId && checkout.isError && (
          <Alert variant="error" title="Couldn't start checkout">
            {errorMessage(checkout.error, "Couldn't start checkout. Please try again.")}
          </Alert>
        )}

        {clientSecret && (
          <div className={styles.paymentBoundary}>
            {(() => {
              const liveStatus = booking.data?.status;
              const copy = liveStatus ? bookingStatusCopy(liveStatus) : null;

              // Once the booking leaves PendingPayment (Confirmed/Expired/Cancelled/etc.), the
              // outcome is already decided — stop showing a payment form for it entirely,
              // regardless of which branch below would otherwise have rendered.
              if (liveStatus && liveStatus !== 'PendingPayment') {
                return copy ? (
                  <Alert variant={copy.variant} title={copy.title}>
                    {copy.description}
                  </Alert>
                ) : (
                  <p className={styles.status} role="status">
                    Booking status: {liveStatus}
                  </p>
                );
              }

              return (
                <>
                  {stripePublishableKey ? (
                    <TrialPaymentForm publishableKey={stripePublishableKey} clientSecret={clientSecret} />
                  ) : (
                    <Alert variant="info" title="Payment isn't configured in this environment">
                      Your time is reserved and a real payment session was created with the backend — this
                      environment just doesn&apos;t have a live payment processor configured, so there&apos;s no way
                      to actually pay yet. Nothing will be charged, and no booking will be confirmed, until that
                      exists.
                    </Alert>
                  )}
                  {copy && (
                    <Alert variant={copy.variant} title={copy.title}>
                      {copy.description}
                    </Alert>
                  )}
                </>
              );
            })()}
          </div>
        )}

        <ButtonLink href={tutorSlug ? `/tutors/${tutorSlug}` : '/find-tutors'} variant="ghost">
          Back to {tutorSlug ? 'tutor profile' : 'Find Tutors'}
        </ButtonLink>
      </Card>

      <AuthGateModal
        open={authGateOpen}
        onOpenChange={setAuthGateOpen}
        action="book a trial lesson"
        returnTo={typeof window !== 'undefined' ? window.location.pathname + window.location.search : undefined}
      />
    </PageContainer>
  );
}
