import { beforeEach, describe, expect, it } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useRecentlyViewedTutors } from "@/features/discovery/hooks/useRecentlyViewedTutors";

describe("useRecentlyViewedTutors", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("starts empty", () => {
    const { result } = renderHook(() => useRecentlyViewedTutors());

    expect(result.current.recentIds).toEqual([]);
  });

  it("records a view most-recent-first", () => {
    const { result } = renderHook(() => useRecentlyViewedTutors());

    act(() => result.current.recordView("a"));
    act(() => result.current.recordView("b"));

    expect(result.current.recentIds).toEqual(["b", "a"]);
  });

  it("moves an already-recent id back to the front instead of duplicating it", () => {
    const { result } = renderHook(() => useRecentlyViewedTutors());

    act(() => result.current.recordView("a"));
    act(() => result.current.recordView("b"));
    act(() => result.current.recordView("a"));

    expect(result.current.recentIds).toEqual(["a", "b"]);
  });

  it("caps the list at 10 entries", () => {
    const { result } = renderHook(() => useRecentlyViewedTutors());

    act(() => {
      for (let i = 0; i < 12; i += 1) {
        result.current.recordView(`tutor-${i}`);
      }
    });

    expect(result.current.recentIds).toHaveLength(10);
    expect(result.current.recentIds[0]).toBe("tutor-11");
    expect(result.current.recentIds).not.toContain("tutor-0");
    expect(result.current.recentIds).not.toContain("tutor-1");
  });
});
