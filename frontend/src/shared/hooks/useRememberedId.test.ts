import { beforeEach, describe, expect, it } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useRememberedId } from "@/shared/hooks/useRememberedId";

describe("useRememberedId", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("starts undefined when nothing has been remembered yet", () => {
    const { result } = renderHook(() => useRememberedId("student"));

    expect(result.current.id).toBeUndefined();
  });

  it("persists the id across hook instances once remembered", () => {
    const { result, unmount } = renderHook(() => useRememberedId("tutor"));

    act(() => result.current.remember("11111111-1111-1111-1111-111111111111"));
    expect(result.current.id).toBe("11111111-1111-1111-1111-111111111111");

    unmount();
    const { result: second } = renderHook(() => useRememberedId("tutor"));
    expect(second.current.id).toBe("11111111-1111-1111-1111-111111111111");
  });

  it("clears the id on forget", () => {
    const { result } = renderHook(() => useRememberedId("parentGuardian"));

    act(() => result.current.remember("22222222-2222-2222-2222-222222222222"));
    act(() => result.current.forget());

    expect(result.current.id).toBeUndefined();
    expect(window.localStorage.getItem("tutorflow.rememberedId.parentGuardian")).toBeNull();
  });

  it("keeps each kind's id independent", () => {
    const { result: tutorResult } = renderHook(() => useRememberedId("tutor"));
    const { result: studentResult } = renderHook(() => useRememberedId("student"));

    act(() => tutorResult.current.remember("t-1"));

    expect(tutorResult.current.id).toBe("t-1");
    expect(studentResult.current.id).toBeUndefined();
  });
});
