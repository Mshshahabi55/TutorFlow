import { beforeEach, describe, expect, it } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useFavoriteTutors } from "@/features/discovery/hooks/useFavoriteTutors";

const TUTOR_A = "11111111-1111-1111-1111-111111111111";
const TUTOR_B = "22222222-2222-2222-2222-222222222222";

describe("useFavoriteTutors", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("starts with no favorites", () => {
    const { result } = renderHook(() => useFavoriteTutors());

    expect(result.current.favoriteIds).toEqual([]);
    expect(result.current.isFavorite(TUTOR_A)).toBe(false);
  });

  it("toggles a Tutor into favorites and back out", () => {
    const { result } = renderHook(() => useFavoriteTutors());

    act(() => result.current.toggleFavorite(TUTOR_A));
    expect(result.current.isFavorite(TUTOR_A)).toBe(true);
    expect(result.current.favoriteIds).toEqual([TUTOR_A]);

    act(() => result.current.toggleFavorite(TUTOR_A));
    expect(result.current.isFavorite(TUTOR_A)).toBe(false);
    expect(result.current.favoriteIds).toEqual([]);
  });

  it("persists across hook instances", () => {
    const { result, unmount } = renderHook(() => useFavoriteTutors());
    act(() => result.current.toggleFavorite(TUTOR_A));
    act(() => result.current.toggleFavorite(TUTOR_B));
    unmount();

    const { result: second } = renderHook(() => useFavoriteTutors());
    expect(second.current.favoriteIds).toEqual([TUTOR_A, TUTOR_B]);
  });

  it("ignores malformed localStorage content rather than throwing", () => {
    window.localStorage.setItem("tutorflow.favoriteTutorIds", "not json");

    const { result } = renderHook(() => useFavoriteTutors());

    expect(result.current.favoriteIds).toEqual([]);
  });
});
