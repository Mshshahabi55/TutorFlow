'use client';

import { FormEvent, useEffect, useState } from 'react';
import { loadStripe, Stripe } from '@stripe/stripe-js';
import { Elements, PaymentElement, useElements, useStripe } from '@stripe/react-stripe-js';
import { Alert } from '@/components/ui/Alert';
import { Button } from '@/components/ui/Button';
import styles from './TrialPaymentForm.module.css';

// Stripe's own documented pattern: load once, outside the component, never recreated on
// re-render (recreating it would remount Elements and lose in-progress input).
let stripePromise: Promise<Stripe | null> | null = null;
function getStripe(publishableKey: string): Promise<Stripe | null> {
  stripePromise ??= loadStripe(publishableKey);
  return stripePromise;
}

export interface TrialPaymentFormProps {
  publishableKey: string;
  clientSecret: string;
}

/**
 * Mounts Stripe's real Payment Element against the already-created PaymentIntent's client
 * secret (ADR-0027 §7) — the P0 gap this closes. This component only ever orchestrates
 * Stripe's own client-side confirmation; it never marks the booking as confirmed itself. The
 * authoritative confirmation path remains entirely server-side (Stripe webhook -> Payments ->
 * ConfirmBooking -> BookingConfirmed -> Lessons) — the parent page's existing GetBooking poll is
 * what the student actually sees transition to "confirmed," never anything decided here.
 */
export function TrialPaymentForm({ publishableKey, clientSecret }: TrialPaymentFormProps) {
  return (
    <Elements stripe={getStripe(publishableKey)} options={{ clientSecret }}>
      <PaymentFormInner />
    </Elements>
  );
}

function PaymentFormInner() {
  const stripe = useStripe();
  const elements = useElements();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [awaitingConfirmation, setAwaitingConfirmation] = useState(false);

  // Some payment methods complete via a full-page redirect rather than inline confirmation
  // (Stripe's own documented `redirect: 'if_required'` contract, not a TutorFlow-specific
  // mechanism) — on return, the client secret arrives as a query param and the PaymentIntent's
  // status must be re-checked, never assumed from the redirect alone.
  useEffect(() => {
    if (!stripe) return;

    const redirectSecret = new URLSearchParams(window.location.search).get('payment_intent_client_secret');
    if (!redirectSecret) return;

    stripe.retrievePaymentIntent(redirectSecret).then(({ paymentIntent }) => {
      if (paymentIntent?.status === 'succeeded' || paymentIntent?.status === 'processing') {
        setAwaitingConfirmation(true);
      } else if (paymentIntent?.status === 'requires_payment_method') {
        setErrorMessage("That payment didn't go through. Please try again.");
      }
    });
  }, [stripe]);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (!stripe || !elements || isSubmitting) {
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    const { error } = await stripe.confirmPayment({
      elements,
      confirmParams: { return_url: window.location.href },
      redirect: 'if_required',
    });

    if (error) {
      setErrorMessage(
        error.type === 'card_error' || error.type === 'validation_error'
          ? (error.message ?? 'Your payment could not be processed. Please try again.')
          : 'Something went wrong processing your payment. Please try again.',
      );
      setIsSubmitting(false);
      return;
    }

    // confirmPayment returning without an error means Stripe accepted the submission — it does
    // NOT mean the Booking is Confirmed. Only the parent page's existing webhook-driven poll
    // (ADR-0027 §7) decides that.
    setAwaitingConfirmation(true);
  };

  if (awaitingConfirmation) {
    return (
      <Alert variant="info" title="Confirming your payment">
        This updates automatically once your payment finishes processing — no need to refresh.
      </Alert>
    );
  }

  return (
    <form onSubmit={handleSubmit} className={styles.form}>
      <PaymentElement />
      {errorMessage && <Alert variant="error" title="Payment couldn't be completed">{errorMessage}</Alert>}
      <Button type="submit" loading={isSubmitting} disabled={!stripe || !elements} fullWidth>
        Pay now
      </Button>
    </form>
  );
}
