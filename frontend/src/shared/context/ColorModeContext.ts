import { createContext } from "react";

/** "system" defers to the OS/browser's own `prefers-color-scheme`; "light"/"dark" is an explicit user choice that overrides it. */
export type ColorMode = "light" | "dark" | "system";

export interface ColorModeContextValue {
  /** The user's stored choice, or "system" if none was ever made. */
  mode: ColorMode;
  /** The actual palette in effect right now — "system" already resolved against the live OS/browser preference. */
  resolvedMode: "light" | "dark";
  setMode: (mode: ColorMode) => void;
}

export const ColorModeContext = createContext<ColorModeContextValue | undefined>(undefined);
