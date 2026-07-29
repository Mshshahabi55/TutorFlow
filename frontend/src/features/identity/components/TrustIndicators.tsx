import type { ReactNode } from "react";
import { Stack, Typography } from "@mui/material";
import VerifiedRoundedIcon from "@mui/icons-material/VerifiedRounded";
import TranslateRoundedIcon from "@mui/icons-material/TranslateRounded";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import type { TutorDto } from "@/services/api/dtos";

export interface TrustIndicatorsProps {
  tutor: TutorDto;
  /**
   * A Tehran-formatted label for the Tutor's soonest open, non-consumed
   * Availability Slot (e.g. `"Sat, Aug 01"`), computed by the caller from
   * the same `useTutorAvailabilitySlots` data `AvailabilityPreviewSection`
   * already fetches — this component makes no query of its own. Omitted
   * (`undefined`/`null`) whenever the caller has no slot data (e.g.
   * `TutorCard`, which would otherwise need one HTTP request per card in a
   * search grid) or the Tutor has no open slot right now; the indicator
   * itself simply doesn't render rather than showing a discouraging "no
   * availability" message in what is meant to be a reassuring trust row.
   */
  nextAvailableLabel?: string | null;
  /**
   * Set `false` when the caller already shows the Tutor's language as its
   * own dedicated badge elsewhere (e.g. `TutorProfileHero`, Phase 4 — a
   * "language badge" is its own explicit ask, distinct from this row) so
   * the same fact isn't shown twice in two different visual treatments.
   * Defaults to `true` for every other caller.
   */
  showLanguage?: boolean;
}

/**
 * A single row of REAL trust facts only — never a placeholder or invented
 * value. `TutorDto` has no education, certification, response-time, or
 * video-intro field, so those (named only as *examples* in the brief this
 * component implements) are simply not rendered; adding them would require
 * new Domain fields, a decision this UI-only phase is not authorized to
 * make. Each fact renders independently and the whole row renders nothing
 * if none apply, so this component is always safe to mount even for a
 * bare-minimum Tutor record.
 */
export function TrustIndicators({ tutor, nextAvailableLabel, showLanguage = true }: TrustIndicatorsProps) {
  const items: { key: string; icon: ReactNode; label: string }[] = [];

  if (tutor.isApproved) {
    items.push({
      key: "verified",
      icon: <VerifiedRoundedIcon fontSize="small" color="primary" aria-hidden="true" />,
      label: "Verified",
    });
  }

  if (showLanguage && tutor.language) {
    items.push({
      key: "language",
      icon: <TranslateRoundedIcon fontSize="small" color="action" aria-hidden="true" />,
      label: `Speaks ${tutor.language}`,
    });
  }

  if (nextAvailableLabel) {
    items.push({
      key: "availability",
      icon: <EventAvailableRoundedIcon fontSize="small" color="success" aria-hidden="true" />,
      label: `Next available ${nextAvailableLabel}`,
    });
  }

  if (items.length === 0) {
    return null;
  }

  return (
    <Stack direction="row" spacing={2.5} flexWrap="wrap" rowGap={1} aria-label="Trust indicators">
      {items.map((item) => (
        <Stack key={item.key} direction="row" spacing={0.5} alignItems="center">
          {item.icon}
          <Typography variant="body2" fontWeight={600} color="text.primary">
            {item.label}
          </Typography>
        </Stack>
      ))}
    </Stack>
  );
}
