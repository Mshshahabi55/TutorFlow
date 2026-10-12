import { renderHook, act } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { useSearchFilters } from './useSearchFilters';

const push = vi.fn();
const replace = vi.fn();
let currentSearchParams = new URLSearchParams();

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push, replace }),
  usePathname: () => '/find-tutors',
  useSearchParams: () => currentSearchParams,
}));

describe('useSearchFilters', () => {
  beforeEach(() => {
    push.mockClear();
    replace.mockClear();
    currentSearchParams = new URLSearchParams();
  });

  it('parses the current filters from the URL', () => {
    currentSearchParams = new URLSearchParams('subjectId=math-id&page=2');
    const { result } = renderHook(() => useSearchFilters());

    expect(result.current.filters.subjectId).toBe('math-id');
    expect(result.current.filters.page).toBe(2);
  });

  it('a discrete filter change pushes a new history entry (so Back/Forward works)', () => {
    const { result } = renderHook(() => useSearchFilters());

    act(() => result.current.updateFilters({ subjectId: 'spanish-id' }));

    expect(push).toHaveBeenCalledWith('/find-tutors?subjectId=spanish-id', { scroll: false });
    expect(replace).not.toHaveBeenCalled();
  });

  it('a replace:true change (debounced text input) replaces instead of pushing', () => {
    const { result } = renderHook(() => useSearchFilters());

    act(() => result.current.updateFilters({ q: 'maria' }, { replace: true }));

    expect(replace).toHaveBeenCalledWith('/find-tutors?q=maria', { scroll: false });
    expect(push).not.toHaveBeenCalled();
  });

  it('changing any filter other than page resets page back to 1', () => {
    currentSearchParams = new URLSearchParams('page=5');
    const { result } = renderHook(() => useSearchFilters());

    act(() => result.current.updateFilters({ subjectId: 'math-id' }));

    expect(push).toHaveBeenCalledWith('/find-tutors?subjectId=math-id', { scroll: false });
  });

  it('changing only the page does not reset itself back to 1', () => {
    currentSearchParams = new URLSearchParams('subjectId=math-id');
    const { result } = renderHook(() => useSearchFilters());

    act(() => result.current.updateFilters({ page: 3 }));

    expect(push).toHaveBeenCalledWith('/find-tutors?subjectId=math-id&page=3', { scroll: false });
  });

  it('resetFilters navigates to the bare pathname with no query string', () => {
    currentSearchParams = new URLSearchParams('subjectId=math-id&page=3');
    const { result } = renderHook(() => useSearchFilters());

    act(() => result.current.resetFilters());

    expect(push).toHaveBeenCalledWith('/find-tutors', { scroll: false });
  });
});
