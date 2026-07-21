import { describe, expect, it } from "vitest";
import { act, renderHook } from "@testing-library/react";
import { usePagination, DEFAULT_PAGE, DEFAULT_PAGE_SIZE } from "@/shared/hooks/usePagination";

describe("usePagination", () => {
  it("starts at the default page and page size", () => {
    const { result } = renderHook(() => usePagination());

    expect(result.current.page).toBe(DEFAULT_PAGE);
    expect(result.current.pageSize).toBe(DEFAULT_PAGE_SIZE);
  });

  it("accepts a custom initial page size", () => {
    const { result } = renderHook(() => usePagination(50));

    expect(result.current.pageSize).toBe(50);
  });

  it("updates page and pageSize independently", () => {
    const { result } = renderHook(() => usePagination());

    act(() => result.current.setPage(3));
    expect(result.current.page).toBe(3);

    act(() => result.current.setPageSize(10));
    expect(result.current.pageSize).toBe(10);
    expect(result.current.page).toBe(3);
  });

  it("resets to the initial page and page size", () => {
    const { result } = renderHook(() => usePagination(50));

    act(() => {
      result.current.setPage(5);
      result.current.setPageSize(10);
    });
    act(() => result.current.reset());

    expect(result.current.page).toBe(DEFAULT_PAGE);
    expect(result.current.pageSize).toBe(50);
  });
});
