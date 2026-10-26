import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { AvailabilityStep } from './AvailabilityStep';
import { TutorApplication } from '../types';
import { AvailabilityConfiguration, BookingSettingsInput, WeeklyRuleInput } from '@/features/availability/types';

/** Regression coverage for the confirmed, live-reproduced defect in
 * docs/product/preply-reverse-engineering/27-phase3-audit-completion-2026-10-25.md §2.3: saving
 * availability persisted correctly to the backend, but navigating away from this step and back
 * (no full page reload) re-rendered the stale pre-save snapshot. `AvailabilityStep.test.tsx`'s 17
 * tests all mock `useCurrentAvailabilityConfiguration` directly, so they never exercise React
 * Query's real cache/invalidation behavior — exactly why that defect shipped undetected. This file
 * mocks only the network boundary (`@/features/availability/api`, the same seam
 * `useMessages.test.tsx` mocks for its own hooks), using the *real* `useCurrentAvailabilityConfiguration`
 * / `useReplaceWeeklyRules` / `useUpdateBookingSettings` hooks against one shared `QueryClient`
 * whose `staleTime` matches the app's real default (`app/providers.tsx`) — without that match, a
 * remount would refetch regardless of whether invalidation ever fired, and the test would pass
 * even against the original, broken code.
 *
 * The mocked `api.ts` functions are backed by a small in-memory fake "backend" (see
 * `createFakeAvailabilityBackend` below) rather than canned one-shot return values, because a
 * successful mutation's own `onSuccess` invalidation triggers an *immediate* refetch while the
 * step is still mounted (React Query refetches active queries on invalidation) — a canned
 * `mockResolvedValueOnce` queue gets consumed by that refetch before the later
 * unmount/remount this test cares about ever happens. A fake backend that always reflects the
 * latest write is immune to exactly how many times a refetch happens to fire, which is also a
 * closer analog of the real server than scripting exact call counts. */

const mockGetCurrentAvailabilityConfiguration = vi.fn();
const mockReplaceWeeklyRules = vi.fn();
const mockUpdateBookingSettings = vi.fn();
vi.mock('@/features/availability/api', () => ({
  getCurrentAvailabilityConfiguration: (...args: unknown[]) => mockGetCurrentAvailabilityConfiguration(...args),
  replaceWeeklyRules: (...args: unknown[]) => mockReplaceWeeklyRules(...args),
  updateBookingSettings: (...args: unknown[]) => mockUpdateBookingSettings(...args),
}));

const baseApplication = { availabilitySettingsConfigured: false } as unknown as TutorApplication;

const DAY_NUMBER_TO_NAME = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

function createFakeAvailabilityBackend(initial: AvailabilityConfiguration) {
  let state = initial;

  mockGetCurrentAvailabilityConfiguration.mockImplementation(() => Promise.resolve(state));
  mockReplaceWeeklyRules.mockImplementation((rules: WeeklyRuleInput[]) => {
    state = {
      ...state,
      weeklyRules: rules.map((r, i) => ({
        id: `fake-${i}`,
        dayOfWeek: DAY_NUMBER_TO_NAME[r.dayOfWeek],
        startTime: `${r.startTime}:00`,
        endTime: `${r.endTime}:00`,
      })),
    };
    return Promise.resolve(undefined);
  });
  mockUpdateBookingSettings.mockImplementation((settings: BookingSettingsInput) => {
    state = { ...state, bookingSettings: { ...settings, isConfigured: true } };
    return Promise.resolve(undefined);
  });

  return {
    fail: (fn: 'replaceWeeklyRules' | 'updateBookingSettings', error: Error) => {
      (fn === 'replaceWeeklyRules' ? mockReplaceWeeklyRules : mockUpdateBookingSettings).mockImplementationOnce(() =>
        Promise.reject(error),
      );
    },
  };
}

const UNCONFIGURED: AvailabilityConfiguration = {
  tutorUserId: 't1',
  weeklyRules: [],
  bookingSettings: { timeZoneId: 'UTC', minNoticeHours: 24, maxBookingWindowDays: 30, defaultSlotDurationMinutes: 60, isConfigured: false },
  isBookable: false,
};

function selectTimezone(name: string) {
  fireEvent.click(screen.getByRole('combobox', { name: 'Timezone' }));
  fireEvent.click(screen.getByRole('option', { name }));
}

function renderStepWithSharedClient(queryClient: QueryClient, onContinue = vi.fn()) {
  function wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  }
  return render(
    <AvailabilityStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />,
    { wrapper },
  );
}

describe('AvailabilityStep — real React Query cache invalidation', () => {
  beforeEach(() => {
    mockGetCurrentAvailabilityConfiguration.mockReset();
    mockReplaceWeeklyRules.mockReset();
    mockUpdateBookingSettings.mockReset();
  });

  it('shows the just-saved configuration (not the stale pre-save snapshot) after navigating away and back without a full reload', async () => {
    // Mirrors `app/providers.tsx`'s real default — without this, React Query's default
    // staleTime of 0 would refetch on every remount regardless of invalidation, masking the bug.
    const queryClient = new QueryClient({
      defaultOptions: { queries: { staleTime: 30_000, retry: false }, mutations: { retry: false } },
    });
    createFakeAvailabilityBackend(UNCONFIGURED);

    const { unmount } = renderStepWithSharedClient(queryClient);

    await waitFor(() => expect(screen.getAllByLabelText('Start time')).toHaveLength(7));
    expect(screen.getAllByLabelText('Start time')[0]).toHaveValue('09:00');

    fireEvent.change(screen.getAllByLabelText('Start time')[0], { target: { value: '10:00' } });
    selectTimezone('Europe/London');
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    await waitFor(() => expect(mockReplaceWeeklyRules).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(mockUpdateBookingSettings).toHaveBeenCalledTimes(1));

    // Simulate navigating away (the wizard unmounts this step on continue) and back, with the
    // same QueryClient persisting across it — exactly as it does between real wizard steps.
    unmount();
    renderStepWithSharedClient(queryClient);

    // The save sends the full 7-row set (only row 0 was edited) — "full replace" semantics,
    // unchanged by this fix — so the fake backend now holds 7 rules with row 0 at 10:00.
    await waitFor(() => expect(screen.getAllByLabelText('Start time')[0]).toHaveValue('10:00'));
    expect(screen.getAllByLabelText('Start time')).toHaveLength(7);
    expect(screen.getByRole('combobox', { name: 'Timezone' })).toHaveTextContent('Europe/London');
  }, 10_000);

  it('does not call onContinue, and does not swallow the failure, when the second mutation fails after the first succeeds', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { staleTime: 30_000, retry: false }, mutations: { retry: false } },
    });
    const backend = createFakeAvailabilityBackend(UNCONFIGURED);
    backend.fail('updateBookingSettings', new Error('boom'));
    const onContinue = vi.fn();

    renderStepWithSharedClient(queryClient, onContinue);

    await waitFor(() => expect(screen.getAllByLabelText('Start time')).toHaveLength(7));
    selectTimezone('Europe/London');
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    await waitFor(() => expect(mockUpdateBookingSettings).toHaveBeenCalledTimes(1));
    await waitFor(() =>
      expect(screen.getByText("Couldn't save your availability. Check your weekly hours and try again.")).toBeInTheDocument(),
    );
    expect(onContinue).not.toHaveBeenCalled();
    expect(mockReplaceWeeklyRules).toHaveBeenCalledTimes(1);
  }, 10_000);
});
