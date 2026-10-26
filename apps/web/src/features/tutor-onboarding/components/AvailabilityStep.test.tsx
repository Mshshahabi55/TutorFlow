import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { AvailabilityStep } from './AvailabilityStep';
import { TutorApplication } from '../types';
import { AvailabilityConfiguration } from '@/features/availability/types';

const mockUseCurrentAvailabilityConfiguration = vi.fn();
const mockReplaceWeeklyRules = vi.fn();
const mockUpdateBookingSettings = vi.fn();
vi.mock('@/features/availability/useAvailability', () => ({
  useCurrentAvailabilityConfiguration: (...args: unknown[]) => mockUseCurrentAvailabilityConfiguration(...args),
  useReplaceWeeklyRules: () => ({ mutateAsync: mockReplaceWeeklyRules, isPending: false }),
  useUpdateBookingSettings: () => ({ mutateAsync: mockUpdateBookingSettings, isPending: false }),
}));

const baseApplication = {
  availabilitySettingsConfigured: false,
} as unknown as TutorApplication;

const UNCONFIGURED: AvailabilityConfiguration = {
  tutorUserId: 't1',
  weeklyRules: [],
  bookingSettings: { timeZoneId: 'UTC', minNoticeHours: 24, maxBookingWindowDays: 30, defaultSlotDurationMinutes: 60, isConfigured: false },
  isBookable: false,
};

const SAVED: AvailabilityConfiguration = {
  tutorUserId: 't1',
  weeklyRules: [
    { id: 'r1', dayOfWeek: 'Tuesday', startTime: '13:00:00', endTime: '15:30:00' },
    { id: 'r2', dayOfWeek: 'Thursday', startTime: '08:00:00', endTime: '10:00:00' },
  ],
  bookingSettings: { timeZoneId: 'Europe/Helsinki', minNoticeHours: 12, maxBookingWindowDays: 14, defaultSlotDurationMinutes: 45, isConfigured: true },
  isBookable: true,
};

function mockLoaded(config: AvailabilityConfiguration) {
  mockUseCurrentAvailabilityConfiguration.mockReturnValue({ data: config, isLoading: false, isError: false, refetch: vi.fn() });
}

function renderStep(application = baseApplication, onContinue = vi.fn()) {
  return render(<AvailabilityStep application={application} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);
}

describe('AvailabilityStep', () => {
  beforeEach(() => {
    mockUseCurrentAvailabilityConfiguration.mockReset();
    mockReplaceWeeklyRules.mockReset().mockResolvedValue(undefined);
    mockUpdateBookingSettings.mockReset().mockResolvedValue(undefined);
    mockLoaded(UNCONFIGURED);
  });

  it('shows a loading state while the current configuration is being fetched, with no save control available', () => {
    mockUseCurrentAvailabilityConfiguration.mockReturnValue({ data: undefined, isLoading: true, isError: false, refetch: vi.fn() });

    renderStep();

    expect(screen.getByText('Loading your availability…')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save & continue' })).not.toBeInTheDocument();
  });

  it('shows a retryable error state if the current configuration fails to load, with no save control available', () => {
    const refetch = vi.fn();
    mockUseCurrentAvailabilityConfiguration.mockReturnValue({ data: undefined, isLoading: false, isError: true, refetch });

    renderStep();

    expect(screen.getByText("Couldn't load your availability")).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save & continue' })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));
    expect(refetch).toHaveBeenCalledTimes(1);
  });

  it('uses the default 7-day 09:00–17:00 template only for a genuine first-time configuration (no saved rules, not configured)', () => {
    mockLoaded(UNCONFIGURED);

    renderStep();

    const startTimes = screen.getAllByLabelText('Start time');
    expect(startTimes).toHaveLength(7);
    startTimes.forEach((input) => expect(input).toHaveValue('09:00'));
    expect(screen.getByLabelText('Minimum notice (hours)')).toHaveValue(24);
    expect(screen.getByLabelText('Booking window (days)')).toHaveValue(30);
    expect(screen.getByLabelText('Default lesson length (minutes)')).toHaveValue(60);
  });

  it('loads and displays an existing non-default saved schedule and timezone when revisiting the step', () => {
    mockLoaded(SAVED);

    renderStep();

    const startTimes = screen.getAllByLabelText('Start time');
    const endTimes = screen.getAllByLabelText('End time');
    expect(startTimes).toHaveLength(2);
    expect(startTimes[0]).toHaveValue('13:00');
    expect(endTimes[0]).toHaveValue('15:30');
    expect(startTimes[1]).toHaveValue('08:00');
    expect(endTimes[1]).toHaveValue('10:00');
    expect(screen.getByLabelText('Minimum notice (hours)')).toHaveValue(12);
    expect(screen.getByLabelText('Booking window (days)')).toHaveValue(14);
    expect(screen.getByLabelText('Default lesson length (minutes)')).toHaveValue(45);
  });

  it('continuing without edits replaces the saved schedule with itself, not the generic default — the core regression', async () => {
    mockLoaded(SAVED);
    const onContinue = vi.fn();

    renderStep(baseApplication, onContinue);

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    await waitFor(() => expect(onContinue).toHaveBeenCalledTimes(1));
    expect(mockReplaceWeeklyRules).toHaveBeenCalledWith([
      { dayOfWeek: 2, startTime: '13:00', endTime: '15:30' },
      { dayOfWeek: 4, startTime: '08:00', endTime: '10:00' },
    ]);
    expect(mockUpdateBookingSettings).toHaveBeenCalledWith({
      timeZoneId: 'Europe/Helsinki',
      minNoticeHours: 12,
      maxBookingWindowDays: 14,
      defaultSlotDurationMinutes: 45,
    });
  });

  it('a deliberate edit after loading a saved schedule is retained and submitted correctly', async () => {
    mockLoaded(SAVED);
    const onContinue = vi.fn();

    renderStep(baseApplication, onContinue);

    fireEvent.change(screen.getAllByLabelText('Start time')[0], { target: { value: '14:00' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    await waitFor(() => expect(onContinue).toHaveBeenCalledTimes(1));
    expect(mockReplaceWeeklyRules).toHaveBeenCalledWith([
      { dayOfWeek: 2, startTime: '14:00', endTime: '15:30' },
      { dayOfWeek: 4, startTime: '08:00', endTime: '10:00' },
    ]);
  });

  it('a late-arriving background refetch of the same query does not erase an edit already made this visit', () => {
    mockLoaded(SAVED);
    const { rerender } = renderStep();

    fireEvent.change(screen.getAllByLabelText('Start time')[0], { target: { value: '14:00' } });

    // Simulate React Query's background refetch resolving with a new object reference for the
    // same underlying data — the parent re-renders with a fresh `config`, but this component's
    // own state must not reset, since it was only ever seeded once from the initial load.
    mockLoaded({ ...SAVED, weeklyRules: [...SAVED.weeklyRules] });
    rerender(<AvailabilityStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getAllByLabelText('Start time')[0]).toHaveValue('14:00');
  });

  it('"Add a time block" adds another rule row', () => {
    mockLoaded(UNCONFIGURED);
    renderStep();

    fireEvent.click(screen.getByRole('button', { name: 'Add a time block' }));

    expect(screen.getAllByLabelText('Start time')).toHaveLength(8);
  });

  it('"Remove" deletes a rule row', () => {
    mockLoaded(UNCONFIGURED);
    renderStep();

    fireEvent.click(screen.getAllByRole('button', { name: 'Remove' })[0]);

    expect(screen.getAllByLabelText('Start time')).toHaveLength(6);
  });

  it('changing a time input updates that rule, not the others', () => {
    mockLoaded(UNCONFIGURED);
    renderStep();

    fireEvent.change(screen.getAllByLabelText('Start time')[0], { target: { value: '10:00' } });

    expect(screen.getAllByLabelText('Start time')[0]).toHaveValue('10:00');
    expect(screen.getAllByLabelText('Start time')[1]).toHaveValue('09:00');
  });

  it('requires a timezone before continuing — never silently calls the save APIs without one', async () => {
    mockLoaded(UNCONFIGURED);
    const onContinue = vi.fn();
    renderStep(baseApplication, onContinue);

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    await waitFor(() => expect(screen.getByText('Select your timezone.')).toBeInTheDocument());
    expect(mockReplaceWeeklyRules).not.toHaveBeenCalled();
    expect(mockUpdateBookingSettings).not.toHaveBeenCalled();
    expect(onContinue).not.toHaveBeenCalled();
  });

  it('shows an informational notice when availability was already configured previously', () => {
    mockLoaded(SAVED);
    renderStep({ ...baseApplication, availabilitySettingsConfigured: true });

    expect(screen.getByText('Availability already configured — saving again replaces it.')).toBeInTheDocument();
  });

  it('does not show the already-configured notice for a fresh draft', () => {
    mockLoaded(UNCONFIGURED);
    renderStep();

    expect(screen.queryByText(/already configured/)).not.toBeInTheDocument();
  });

  it('calls onBack when Back is clicked', () => {
    mockLoaded(UNCONFIGURED);
    const onBack = vi.fn();
    render(<AvailabilityStep application={baseApplication} onContinue={vi.fn()} onBack={onBack} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Back' }));

    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it("surfaces the parent's save error alongside its own local validation error", () => {
    mockLoaded(UNCONFIGURED);
    render(<AvailabilityStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error="Couldn't save the draft." />);

    expect(screen.getByText("Couldn't save the draft.")).toBeInTheDocument();
  });

  it('has no detectable axe violations once loaded', async () => {
    mockLoaded(UNCONFIGURED);
    const { container } = renderStep();
    expect((await axe(container)).violations).toEqual([]);
  });

  it('has no detectable axe violations while loading', async () => {
    mockUseCurrentAvailabilityConfiguration.mockReturnValue({ data: undefined, isLoading: true, isError: false, refetch: vi.fn() });
    const { container } = renderStep();
    expect((await axe(container)).violations).toEqual([]);
  });
});
