import { describe, expect, it, beforeEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useCurrentActor } from "@/shared/hooks/useCurrentActor";
import { ActorProvider } from "@/shared/context/ActorProvider";
import type { ReactNode } from "react";

function wrapper({ children }: { children: ReactNode }) {
  return <ActorProvider>{children}</ActorProvider>;
}

describe("useCurrentActor", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("throws when used outside an ActorProvider", () => {
    expect(() => renderHook(() => useCurrentActor())).toThrow(
      "useCurrentActor must be used within an ActorProvider.",
    );
  });

  it("starts with no role selected and isAuthenticated always false", () => {
    const { result } = renderHook(() => useCurrentActor(), { wrapper });

    expect(result.current.actor.role).toBeNull();
    expect(result.current.actor.isAuthenticated).toBe(false);
  });

  it("updates the role when setRole is called, but never becomes authenticated", () => {
    const { result } = renderHook(() => useCurrentActor(), { wrapper });

    act(() => {
      result.current.setRole("Tutor");
    });

    expect(result.current.actor.role).toBe("Tutor");
    expect(result.current.actor.isAuthenticated).toBe(false);
  });

  it("persists the selected role to localStorage across a fresh mount", () => {
    const { result, unmount } = renderHook(() => useCurrentActor(), { wrapper });

    act(() => {
      result.current.setRole("AdminStaff");
    });
    unmount();

    const { result: secondResult } = renderHook(() => useCurrentActor(), { wrapper });

    expect(secondResult.current.actor.role).toBe("AdminStaff");
  });

  it("clears the stored role when setRole(null) is called", () => {
    const { result } = renderHook(() => useCurrentActor(), { wrapper });

    act(() => {
      result.current.setRole("Student");
    });
    act(() => {
      result.current.setRole(null);
    });

    expect(result.current.actor.role).toBeNull();
    expect(window.localStorage.getItem("tutorflow.devActorRole")).toBeNull();
  });
});
