import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  ColorModeContext,
  type ColorMode,
  type ColorModeContextValue,
} from "@/shared/context/ColorModeContext";

const STORAGE_KEY = "tutorflow.colorMode";

function readStoredMode(): ColorMode {
  if (typeof window === "undefined") {
    return "system";
  }

  const stored = window.localStorage.getItem(STORAGE_KEY);
  return stored === "light" || stored === "dark" ? stored : "system";
}

function prefersDark(): boolean {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
    return false;
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}

/**
 * Phase D3: the same `localStorage`-backed provider pattern
 * `ActorProvider` already uses (`tutorflow.devActorRole`), applied to
 * light/dark preference (`tutorflow.colorMode`) instead. Deliberately
 * **not** composed into `AppProviders.tsx` yet — see
 * docs/phases/PHASE-D3-REPORT.md for why wiring it into the live app
 * before a `ThemeToggle` control exists anywhere in the real header would
 * silently dark-mode the app for every OS-dark-mode user with no way to
 * switch back. Proven standalone this milestone via the dev-only
 * StyleGuidePage's dark-mode preview section instead.
 */
export function ColorModeProvider({ children }: { children: ReactNode }) {
  const [mode, setModeState] = useState<ColorMode>(readStoredMode);
  const [systemPrefersDark, setSystemPrefersDark] = useState<boolean>(prefersDark);

  useEffect(() => {
    if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
      return;
    }

    const query = window.matchMedia("(prefers-color-scheme: dark)");
    const listener = (event: MediaQueryListEvent) => setSystemPrefersDark(event.matches);
    query.addEventListener("change", listener);
    return () => query.removeEventListener("change", listener);
  }, []);

  const setMode = (nextMode: ColorMode) => {
    setModeState(nextMode);
    if (typeof window !== "undefined") {
      if (nextMode === "system") {
        window.localStorage.removeItem(STORAGE_KEY);
      } else {
        window.localStorage.setItem(STORAGE_KEY, nextMode);
      }
    }
  };

  const resolvedMode: "light" | "dark" = mode === "system" ? (systemPrefersDark ? "dark" : "light") : mode;

  const value = useMemo<ColorModeContextValue>(
    () => ({ mode, resolvedMode, setMode }),
    [mode, resolvedMode],
  );

  return <ColorModeContext.Provider value={value}>{children}</ColorModeContext.Provider>;
}
