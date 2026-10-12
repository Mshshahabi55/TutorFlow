import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { TutorAvailabilityManager } from './TutorAvailabilityManager';
import { AvailabilityConfiguration } from '../types';

const mockUseCurrentAvailabilityConfiguration = vi.fn();
const mockReplaceWeeklyRules = vi.fn();
const mockUpdateBookingSettings = vi.fn();
vi.mock('../useAvailability', () => ({
  useCurrentAvailabilityConfiguration: () => mockUseCurrentAvailabilityConfiguration(),
  useReplaceWeeklyRules: () => mockReplaceWeeklyRules(),
  useUpdateBookingSettings: () => mockUpdateBookingSettings(),
}));

const CONFIG: AvailabilityConfiguration = {
  tutorUserId: 't1',
  weeklyRules: [{ id: 'r1', dayOfWeek: 'Monday', startTime: '09:00:00', endTime: '17:00:00' }],
  bookingSettings: {
    timeZoneId: 'Europe/Helsinki',
    minNoticeHours: 24,
    maxBookingWindowDays: 30,
    defaultSlotDurationMinutes: 50,
    isConfigured: true,
  },
  isBookable: true,
};

describe('TutorAvailabilityManager', () => {
  beforeEach(() => {
    mockUseCurrentAvailabilityConfiguration.mockReturnValue({ data: CONFIG, isLoading: false, isError: false, refetch: vi.fn() });
    mockReplaceWeeklyRules.mockReturnValue({ mutateAsync: vi.fn().mockResolvedValue(undefined), isPending: false, isError: false });
    mockUpdateBookingSettings.mockReturnValue({ mutateAsync: vi.fn().mockResolvedValue(undefined), isPending: false, isError: false });
  });

  it('shows a loading state while the current configuration is being fetched', () => {
    mockUseCurrentAvailabilityConfiguration.mockReturnValue({ data: undefined, isLoading: true, isError: false, refetch: vi.fn() });

    render(<TutorAvailabilityManager />);

    expect(screen.queryByText('Weekly hours')).not.toBeInTheDocument();
  });

  it('shows a retryable error state if the current configuration fails to load', () => {
    const refetch = vi.fn();
    mockUseCurrentAvailabilityConfiguration.mockReturnValue({ data: undefined, isLoading: false, isError: true, refetch });

    render(<TutorAvailabilityManager />);
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));

    expect(refetch).toHaveBeenCalledTimes(1);
  });

  it("prefills the already-saved weekly hours and booking settings — this is the audit's core fix", () => {
    render(<TutorAvailabilityManager />);

    expect(screen.getByLabelText('Start time')).toHaveValue('09:00');
    expect(screen.getByLabelText('End time')).toHaveValue('17:00');
    expect(screen.getByLabelText('Minimum notice (hours)')).toHaveValue(24);
    expect(screen.getByLabelText('Booking window (days)')).toHaveValue(30);
    expect(screen.getByLabelText('Default lesson length (minutes)')).toHaveValue(50);
  });

  it('a tutor with no weekly rules yet sees one empty row to start from, not a blank/broken form', () => {
    mockUseCurrentAvailabilityConfiguration.mockReturnValue({
      data: { ...CONFIG, weeklyRules: [] },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<TutorAvailabilityManager />);

    expect(screen.getByLabelText('Start time')).toBeInTheDocument();
  });

  it('clicking "Save changes" replaces the weekly rules and updates booking settings with the current form values', async () => {
    const replaceMutateAsync = vi.fn().mockResolvedValue(undefined);
    const updateMutateAsync = vi.fn().mockResolvedValue(undefined);
    mockReplaceWeeklyRules.mockReturnValue({ mutateAsync: replaceMutateAsync, isPending: false, isError: false });
    mockUpdateBookingSettings.mockReturnValue({ mutateAsync: updateMutateAsync, isPending: false, isError: false });

    render(<TutorAvailabilityManager />);
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    await vi.waitFor(() => {
      expect(replaceMutateAsync).toHaveBeenCalledWith([{ dayOfWeek: 1, startTime: '09:00', endTime: '17:00' }]);
      expect(updateMutateAsync).toHaveBeenCalledWith({
        timeZoneId: 'Europe/Helsinki',
        minNoticeHours: 24,
        maxBookingWindowDays: 30,
        defaultSlotDurationMinutes: 50,
      });
    });

    expect(await screen.findByText('Saved')).toBeInTheDocument();
  });

  it('a save failure shows a human-friendly error, not a raw technical message', async () => {
    mockReplaceWeeklyRules.mockReturnValue({ mutateAsync: vi.fn().mockRejectedValue(new Error('network blew up')), isPending: false, isError: true });

    render(<TutorAvailabilityManager />);
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    expect(await screen.findByText("Couldn't save your availability")).toBeInTheDocument();
    expect(screen.queryByText('network blew up')).not.toBeInTheDocument();
  });

  it('has no detectable axe violations once loaded', async () => {
    const { container } = render(<TutorAvailabilityManager />);
    expect((await axe(container)).violations).toEqual([]);
  });
});
