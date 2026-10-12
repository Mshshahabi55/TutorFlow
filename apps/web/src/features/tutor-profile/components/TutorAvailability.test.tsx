import { fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { axe } from 'vitest-axe';
import { TutorAvailability } from './TutorAvailability';
import { TutorSlotDto } from '../api';

const mockPush = vi.fn();
vi.mock('next/navigation', () => ({ useRouter: () => ({ push: mockPush }) }));

const mockAvailabilityPreview = vi.fn();
const mockTrialEligibility = vi.fn();
const mockTutorSlots = vi.fn();
vi.mock('../useTutorProfile', () => ({
  useAvailabilityPreview: (...args: unknown[]) => mockAvailabilityPreview(...args),
  useTrialEligibility: (...args: unknown[]) => mockTrialEligibility(...args),
  useTutorSlots: (...args: unknown[]) => mockTutorSlots(...args),
}));

// Fixed "now" at noon UTC — comfortably inside the same local calendar day across realistic
// developer/CI timezones, so slot-to-date-bucket assignment is deterministic without hardcoding
// locale-formatted date strings (which TutorAvailability deliberately renders via toLocaleDateString).
const NOW = new Date('2026-01-12T12:00:00.000Z');

function slotAt(offsetHoursFromNow: number, minutes: number): TutorSlotDto {
  const start = new Date(NOW.getTime() + offsetHoursFromNow * 60 * 60 * 1000);
  const end = new Date(start.getTime() + minutes * 60 * 1000);
  return { startUtc: start.toISOString(), endUtc: end.toISOString() };
}

function dateGroup() {
  return screen.getByRole('group', { name: 'Select a date' });
}

function timeSlotGroup() {
  return screen.getByRole('group', { name: 'Select a time' });
}

const onRequireAuth = vi.fn();

const baseProps = {
  tutorUserId: 't1',
  tutorSlug: 'jane-dev',
  isAcceptingNewStudents: true,
  isAuthenticatedStudent: true,
  onRequireAuth,
};

describe('TutorAvailability', () => {
  beforeEach(() => {
    vi.setSystemTime(NOW);
    mockPush.mockClear();
    onRequireAuth.mockClear();
    mockAvailabilityPreview.mockReturnValue({
      data: { hasUpcomingAvailability: true, nextAvailableStartUtc: null, timeZoneId: 'Europe/Helsinki' },
      isLoading: false,
      isError: false,
    });
    mockTrialEligibility.mockReturnValue({ data: { hasUsedTrial: false }, isLoading: false });
    mockTutorSlots.mockReturnValue({ data: undefined, isLoading: true, isError: false, refetch: vi.fn() });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('not accepting new students: shows an explanatory message, never the picker', () => {
    render(<TutorAvailability {...baseProps} isAcceptingNewStudents={false} />);

    expect(screen.getByText("This tutor isn't currently accepting new students.")).toBeInTheDocument();
    expect(screen.queryByText('Trial lesson length')).not.toBeInTheDocument();
  });

  it('no upcoming availability: shows an empty state, not a blank calendar', () => {
    mockAvailabilityPreview.mockReturnValue({
      data: { hasUpcomingAvailability: false, nextAvailableStartUtc: null, timeZoneId: 'Europe/Helsinki' },
      isLoading: false,
      isError: false,
    });

    render(<TutorAvailability {...baseProps} />);

    expect(screen.getByText('No upcoming availability')).toBeInTheDocument();
    expect(screen.queryByRole('group', { name: 'Select a date' })).not.toBeInTheDocument();
  });

  it('guest: shows the tutor timezone and a "Log in to see times" CTA that opens the auth gate, not the real picker', () => {
    render(<TutorAvailability {...baseProps} isAuthenticatedStudent={false} />);

    expect(screen.getByText(/Europe\/Helsinki/)).toBeInTheDocument();
    const cta = screen.getByRole('button', { name: 'Log in to see times' });
    fireEvent.click(cta);

    expect(onRequireAuth).toHaveBeenCalledWith('see exact available times');
    expect(screen.queryByText('Trial lesson length')).not.toBeInTheDocument();
  });

  it('authenticated student who already used their trial: shows an explanation instead of the picker', () => {
    mockTrialEligibility.mockReturnValue({ data: { hasUsedTrial: true }, isLoading: false });

    render(<TutorAvailability {...baseProps} />);

    expect(
      screen.getByText("You've already had a trial lesson with this tutor. Message them to arrange your next lesson."),
    ).toBeInTheDocument();
    expect(screen.queryByText('Trial lesson length')).not.toBeInTheDocument();
  });

  it('eligible student: shows a loading skeleton while slots are loading, not stale/empty content', () => {
    mockTutorSlots.mockReturnValue({ data: undefined, isLoading: true, isError: false, refetch: vi.fn() });

    render(<TutorAvailability {...baseProps} />);

    expect(screen.getByText('Trial lesson length')).toBeInTheDocument();
    expect(screen.queryByRole('group', { name: 'Select a date' })).not.toBeInTheDocument();
  });

  it('eligible student: a slot-loading failure shows a retryable error state', () => {
    const refetch = vi.fn();
    mockTutorSlots.mockReturnValue({ data: undefined, isLoading: false, isError: true, refetch });

    render(<TutorAvailability {...baseProps} />);

    expect(screen.getByText("Couldn't load time slots")).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));

    expect(refetch).toHaveBeenCalledTimes(1);
  });

  it('eligible student: no open slots anywhere in the window disables every date and shows the try-another-date message', () => {
    mockTutorSlots.mockReturnValue({
      data: { timeZoneId: 'Europe/Helsinki', durationMinutes: 50, maxBookingWindowDays: 365, slots: [] },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<TutorAvailability {...baseProps} />);

    const dateButtons = within(dateGroup()).getAllByRole('button');
    expect(dateButtons).toHaveLength(7);
    for (const button of dateButtons) {
      expect(button).toBeDisabled();
    }
    expect(screen.getByText('No available times on this date. Try another date.')).toBeInTheDocument();
  });

  it('eligible student: auto-selects the first date with availability and lists its times', () => {
    const todaySlot = slotAt(2, 50);
    mockTutorSlots.mockReturnValue({
      data: { timeZoneId: 'Europe/Helsinki', durationMinutes: 50, maxBookingWindowDays: 365, slots: [todaySlot] },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<TutorAvailability {...baseProps} />);

    const dateButtons = within(dateGroup()).getAllByRole('button');
    expect(dateButtons[0]).toHaveAttribute('aria-pressed', 'true');
    expect(dateButtons[0]).not.toBeDisabled();
    expect(dateButtons[1]).toBeDisabled();
    expect(within(timeSlotGroup()).getAllByRole('radio')).toHaveLength(1);
  });

  it('selecting a time slot shows the selection summary and enables Continue; Continue navigates to the honest checkout boundary with the full selection', () => {
    const todaySlot = slotAt(2, 50);
    mockTutorSlots.mockReturnValue({
      data: { timeZoneId: 'Europe/Helsinki', durationMinutes: 50, maxBookingWindowDays: 365, slots: [todaySlot] },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<TutorAvailability {...baseProps} />);

    const continueButton = screen.getByRole('button', { name: 'Continue' });
    expect(continueButton).toBeDisabled();

    fireEvent.click(within(timeSlotGroup()).getByRole('radio'));

    expect(screen.getByText(/Selected:/)).toBeInTheDocument();
    expect(continueButton).toBeEnabled();

    fireEvent.click(continueButton);

    expect(mockPush).toHaveBeenCalledTimes(1);
    const url = mockPush.mock.calls[0][0] as string;
    expect(url.startsWith('/bookings/new?')).toBe(true);
    const params = new URLSearchParams(url.split('?')[1]);
    expect(params.get('tutorUserId')).toBe('t1');
    expect(params.get('tutorSlug')).toBe('jane-dev');
    expect(params.get('durationMinutes')).toBe('50');
    expect(params.get('startUtc')).toBe(todaySlot.startUtc);
    expect(params.get('endUtc')).toBe(todaySlot.endUtc);
  });

  it('switching the trial duration re-requests real slots for the new duration (never a client-side filter)', () => {
    mockTutorSlots.mockImplementation(
      (_tutorUserId: string, params: { durationMinutes: number }) => ({
        data: {
          timeZoneId: 'Europe/Helsinki',
          durationMinutes: params.durationMinutes,
          maxBookingWindowDays: 365,
          slots: params.durationMinutes === 50 ? [slotAt(2, 50)] : [slotAt(2, 25), slotAt(3, 25)],
        },
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      }),
    );

    render(<TutorAvailability {...baseProps} />);

    expect(within(timeSlotGroup()).getAllByRole('radio', { name: /(AM|PM)$/ })).toHaveLength(1);

    fireEvent.click(screen.getByRole('radio', { name: '25 min' }));

    expect(mockTutorSlots).toHaveBeenLastCalledWith(
      't1',
      expect.objectContaining({ durationMinutes: 25 }),
      true,
    );
    expect(within(timeSlotGroup()).getAllByRole('radio', { name: /(AM|PM)$/ })).toHaveLength(2);
  });

  it('a slot that disappears from a refetched response (e.g. another student booked it) clears the stale selection', () => {
    const todaySlot = slotAt(2, 50);
    const refetch = vi.fn();
    mockTutorSlots.mockReturnValue({
      data: { timeZoneId: 'Europe/Helsinki', durationMinutes: 50, maxBookingWindowDays: 365, slots: [todaySlot] },
      isLoading: false,
      isError: false,
      refetch,
    });

    const { rerender } = render(<TutorAvailability {...baseProps} />);
    fireEvent.click(within(timeSlotGroup()).getByRole('radio'));
    expect(screen.getByRole('button', { name: 'Continue' })).toBeEnabled();

    mockTutorSlots.mockReturnValue({
      data: { timeZoneId: 'Europe/Helsinki', durationMinutes: 50, maxBookingWindowDays: 365, slots: [] },
      isLoading: false,
      isError: false,
      refetch,
    });
    rerender(<TutorAvailability {...baseProps} />);

    expect(screen.queryByText(/Selected:/)).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Continue' })).toBeDisabled();
  });

  it('has no detectable axe violations once a slot is selected', async () => {
    const todaySlot = slotAt(2, 50);
    mockTutorSlots.mockReturnValue({
      data: { timeZoneId: 'Europe/Helsinki', durationMinutes: 50, maxBookingWindowDays: 365, slots: [todaySlot] },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    const { container } = render(<TutorAvailability {...baseProps} />);
    fireEvent.click(within(timeSlotGroup()).getByRole('radio'));

    expect((await axe(container)).violations).toEqual([]);
  });
});
