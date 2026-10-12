import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { TrialPaymentForm } from './TrialPaymentForm';

const mockConfirmPayment = vi.fn();
const mockRetrievePaymentIntent = vi.fn();
const mockUseStripe = vi.fn();
const mockUseElements = vi.fn();

vi.mock('@stripe/react-stripe-js', () => ({
  Elements: ({ children }: { children: React.ReactNode }) => <div data-testid="elements">{children}</div>,
  PaymentElement: () => <div data-testid="payment-element" />,
  useStripe: () => mockUseStripe(),
  useElements: () => mockUseElements(),
}));

vi.mock('@stripe/stripe-js', () => ({
  loadStripe: vi.fn().mockResolvedValue({}),
}));

describe('TrialPaymentForm', () => {
  beforeEach(() => {
    mockConfirmPayment.mockReset();
    mockRetrievePaymentIntent.mockReset();
    mockUseStripe.mockReturnValue({ confirmPayment: mockConfirmPayment, retrievePaymentIntent: mockRetrievePaymentIntent });
    mockUseElements.mockReturnValue({});
    window.history.replaceState({}, '', '/bookings/new?tutorUserId=t1');
  });

  it('renders the real Payment Element once Stripe is ready', () => {
    render(<TrialPaymentForm publishableKey="pk_test_123" clientSecret="pi_123_secret_abc" />);

    expect(screen.getByTestId('payment-element')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Pay now' })).not.toBeDisabled();
  });

  it('disables submit while Stripe/Elements have not finished loading', () => {
    mockUseStripe.mockReturnValue(null);

    render(<TrialPaymentForm publishableKey="pk_test_123" clientSecret="pi_123_secret_abc" />);

    expect(screen.getByRole('button', { name: 'Pay now' })).toBeDisabled();
  });

  it('clicking "Pay now" calls stripe.confirmPayment with redirect:if_required, never a direct success assertion', async () => {
    mockConfirmPayment.mockResolvedValue({});

    render(<TrialPaymentForm publishableKey="pk_test_123" clientSecret="pi_123_secret_abc" />);
    fireEvent.click(screen.getByRole('button', { name: 'Pay now' }));

    await waitFor(() => {
      expect(mockConfirmPayment).toHaveBeenCalledWith(
        expect.objectContaining({
          redirect: 'if_required',
          confirmParams: expect.objectContaining({ return_url: expect.any(String) }),
        }),
      );
    });
  });

  it('a card decline shows the Stripe-provided user-safe message and re-enables submit', async () => {
    mockConfirmPayment.mockResolvedValue({ error: { type: 'card_error', message: 'Your card was declined.' } });

    render(<TrialPaymentForm publishableKey="pk_test_123" clientSecret="pi_123_secret_abc" />);
    fireEvent.click(screen.getByRole('button', { name: 'Pay now' }));

    expect(await screen.findByText('Your card was declined.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Pay now' })).not.toBeDisabled();
  });

  it('a non-card Stripe error shows a generic friendly message, never the raw internal detail', async () => {
    mockConfirmPayment.mockResolvedValue({ error: { type: 'api_error', message: 'internal stripe diagnostic detail' } });

    render(<TrialPaymentForm publishableKey="pk_test_123" clientSecret="pi_123_secret_abc" />);
    fireEvent.click(screen.getByRole('button', { name: 'Pay now' }));

    expect(await screen.findByText(/Something went wrong processing your payment/)).toBeInTheDocument();
    expect(screen.queryByText('internal stripe diagnostic detail')).not.toBeInTheDocument();
  });

  it('a successful confirmPayment shows a "confirming" state — never claims the booking itself is confirmed', async () => {
    mockConfirmPayment.mockResolvedValue({});

    render(<TrialPaymentForm publishableKey="pk_test_123" clientSecret="pi_123_secret_abc" />);
    fireEvent.click(screen.getByRole('button', { name: 'Pay now' }));

    expect(await screen.findByText('Confirming your payment')).toBeInTheDocument();
    expect(screen.queryByText(/is confirmed/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /mark as paid|simulate payment/i })).not.toBeInTheDocument();
  });

  it('duplicate rapid submission does not call confirmPayment twice', async () => {
    let resolveConfirm: (value: unknown) => void = () => {};
    mockConfirmPayment.mockReturnValue(new Promise((resolve) => { resolveConfirm = resolve; }));

    render(<TrialPaymentForm publishableKey="pk_test_123" clientSecret="pi_123_secret_abc" />);
    const button = screen.getByRole('button', { name: 'Pay now' });
    fireEvent.click(button);
    fireEvent.click(button);
    fireEvent.click(button);

    resolveConfirm({});
    await waitFor(() => expect(screen.getByText('Confirming your payment')).toBeInTheDocument());

    expect(mockConfirmPayment).toHaveBeenCalledTimes(1);
  });
});
