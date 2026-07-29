import { IconButton, Tooltip } from "@mui/material";
import LightModeRoundedIcon from "@mui/icons-material/LightModeRounded";
import DarkModeRoundedIcon from "@mui/icons-material/DarkModeRounded";
import { useColorMode } from "@/shared/hooks/useColorMode";

/** Cycles light/dark from the header's own icon button (`AppHeader.tsx`). */
export function ThemeToggle() {
  const { resolvedMode, setMode } = useColorMode();
  const isDark = resolvedMode === "dark";
  const label = isDark ? "Switch to light mode" : "Switch to dark mode";

  return (
    <Tooltip title={label}>
      <IconButton aria-label={label} onClick={() => setMode(isDark ? "light" : "dark")}>
        {isDark ? <LightModeRoundedIcon /> : <DarkModeRoundedIcon />}
      </IconButton>
    </Tooltip>
  );
}
