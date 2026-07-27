import { describe, expect, it, beforeEach, afterEach, vi } from "vitest";
import { renderHook, act } from "@testing-library/react";
import type { ReactNode } from "react";
import { useColorMode } from "@/shared/hooks/useColorMode";
import { ColorModeProvider } from "@/shared/context/ColorModeProvider";

function wrapper({ children }: { children: ReactNode }) {
  return <ColorModeProvider>{children}</ColorModeProvider>;
}

function mockSystemPrefersDark(matches: boolean) {
  window.matchMedia = vi.fn((query: string): MediaQueryList => ({
    matches,
    media: query,
    onchange: null,
    addEventListener: () => {},
    removeEventListener: () => {},
    addListener: () => {},
    removeListener: () => {},
    dispatchEvent: () => false,
  }));
}

describe("useColorMode", () => {
  beforeEach(() => {
    window.localStorage.clear();
    mockSystemPrefersDark(false);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("throws when used outside a ColorModeProvider", () => {
    expect(() => renderHook(() => useColorMode())).toThrow(
      "useColorMode must be used within a ColorModeProvider.",
    );
  });

  it("defaults to system, resolved against the OS preference (light here)", () => {
    const { result } = renderHook(() => useColorMode(), { wrapper });

    expect(result.current.mode).toBe("system");
    expect(result.current.resolvedMode).toBe("light");
  });

  it("resolves to dark when the OS prefers dark and no explicit choice was made", () => {
    mockSystemPrefersDark(true);
    const { result } = renderHook(() => useColorMode(), { wrapper });

    expect(result.current.mode).toBe("system");
    expect(result.current.resolvedMode).toBe("dark");
  });

  it("an explicit choice overrides the OS preference", () => {
    mockSystemPrefersDark(true);
    const { result } = renderHook(() => useColorMode(), { wrapper });

    act(() => {
      result.current.setMode("light");
    });

    expect(result.current.resolvedMode).toBe("light");
  });

  it("persists an explicit choice to localStorage across a fresh mount", () => {
    const { result, unmount } = renderHook(() => useColorMode(), { wrapper });

    act(() => {
      result.current.setMode("dark");
    });
    unmount();

    const { result: secondResult } = renderHook(() => useColorMode(), { wrapper });

    expect(secondResult.current.mode).toBe("dark");
    expect(secondResult.current.resolvedMode).toBe("dark");
  });

  it("setMode('system') clears the stored explicit choice", () => {
    const { result } = renderHook(() => useColorMode(), { wrapper });

    act(() => {
      result.current.setMode("dark");
    });
    act(() => {
      result.current.setMode("system");
    });

    expect(result.current.mode).toBe("system");
    expect(window.localStorage.getItem("tutorflow.colorMode")).toBeNull();
  });
});
