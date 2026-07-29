import { beforeEach, describe, expect, it } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useSavedSearches } from "@/features/discovery/hooks/useSavedSearches";
import type { SearchTutorsFilters } from "@/features/discovery/api/discoveryService";

const FILTERS: SearchTutorsFilters = { subject: "Math", language: "", location: "", availableFrom: "" };

describe("useSavedSearches", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("starts empty", () => {
    const { result } = renderHook(() => useSavedSearches());

    expect(result.current.savedSearches).toEqual([]);
  });

  it("saves a named search with the given filters", () => {
    const { result } = renderHook(() => useSavedSearches());

    act(() => result.current.saveSearch("Math tutors", FILTERS));

    expect(result.current.savedSearches).toHaveLength(1);
    expect(result.current.savedSearches[0]).toMatchObject({ name: "Math tutors", filters: FILTERS });
  });

  it("removes a saved search by id", () => {
    const { result } = renderHook(() => useSavedSearches());

    act(() => result.current.saveSearch("Math tutors", FILTERS));
    const id = result.current.savedSearches[0].id;
    act(() => result.current.removeSearch(id));

    expect(result.current.savedSearches).toEqual([]);
  });

  it("persists across hook instances", () => {
    const { result, unmount } = renderHook(() => useSavedSearches());
    act(() => result.current.saveSearch("Math tutors", FILTERS));
    unmount();

    const { result: second } = renderHook(() => useSavedSearches());
    expect(second.current.savedSearches).toHaveLength(1);
  });

  it("ignores malformed localStorage content rather than throwing", () => {
    window.localStorage.setItem("tutorflow.savedSearches", "not json");

    const { result } = renderHook(() => useSavedSearches());

    expect(result.current.savedSearches).toEqual([]);
  });
});
