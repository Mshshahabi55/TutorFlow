import { IconButton, Tooltip } from "@mui/material";
import LightModeRoundedIcon from "@mui/icons-material/LightModeRounded";
import DarkModeRoundedIcon from "@mui/icons-material/DarkModeRounded";
import { useColorMode } from "@/shared/hooks/useColorMode";

/**
 * Cycles light/dark. Phase D3: built and unit-tested standalone, previewed
 * only on the dev-only StyleGuidePage — not yet placed in the real
 * AppLayout header (a later milestone wires `ColorModeProvider` into
 * `AppProviders.tsx` and drops this into the live shell; see
 * docs/phases/PHASE-D3-REPORT.md for why that's deliberately deferred).
 */
export function ThemeToggle() {
  const { resolvedMode, setMode } = useColorMode();
  const isDark = resolvedMode === "dark";
  const label = isDark ? "Switch to light mode" : "Switch to dark mode";

  return (
    <Tooltip title={label}>
      <IconButton aria-label={label} size="small" onClick={() => setMode(isDark ? "light" : "dark")}>
        {isDark ? <LightModeRoundedIcon fontSize="small" /> : <DarkModeRoundedIcon fontSize="small" />}
      </IconButton>
    </Tooltip>
  );
}
