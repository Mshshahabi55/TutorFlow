import { Chip, type ChipProps } from "@mui/material";

export type StatusTone = "neutral" | "info" | "success" | "warning" | "critical";

export interface StatusPillProps {
  label: string;
  tone?: StatusTone;
  size?: ChipProps["size"];
}

const toneToColor: Record<StatusTone, ChipProps["color"]> = {
  neutral: "default",
  info: "info",
  success: "success",
  warning: "warning",
  critical: "error",
};

/**
 * A generic status indicator, deliberately not tied to any Domain enum
 * (Session/Tutor/Relationship status mapping is a feature-level concern,
 * added once those features exist) — Foundation only supplies the reusable
 * visual primitive.
 */
export function StatusPill({ label, tone = "neutral", size = "small" }: StatusPillProps) {
  return (
    <Chip
      label={label}
      color={toneToColor[tone]}
      size={size}
      variant={tone === "neutral" ? "outlined" : "filled"}
    />
  );
}
