import type { ReactNode } from "react";
import { Box, Stack, Typography } from "@mui/material";
import { SectionCard } from "@/shared/components/SectionCard";

export interface ProfilePlaceholderSectionProps {
  id?: string;
  /** The `SectionCard`'s own heading (e.g. "Reviews", "Teaching Style"). */
  title: string;
  icon: ReactNode;
  /** The placeholder's own honest headline (e.g. "No reviews yet"). */
  heading: string;
  description: string;
}

/**
 * The one shared "this part of the profile has no real data yet" layout —
 * previously four near-identical copies (Reviews, FAQ, Certificates &
 * Experience, Teaching Style), each its own icon+heading+description stack.
 * A neutral grey icon circle (not the brand-tinted one `EmptyState`/
 * `TutorCard`'s avatar use) deliberately keeps these low-key and
 * informational — they have no call-to-action and nothing to celebrate,
 * unlike an actionable empty state. Never renders a fabricated value in
 * place of missing data; every caller must supply its own honest copy.
 */
export function ProfilePlaceholderSection({ id, title, icon, heading, description }: ProfilePlaceholderSectionProps) {
  return (
    <SectionCard id={id} headingComponent="h2" title={title}>
      <Stack direction="row" spacing={2} alignItems="center">
        <Box
          sx={{
            width: 56,
            height: 56,
            flexShrink: 0,
            borderRadius: "50%",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            bgcolor: (t) => (t.palette.mode === "dark" ? "grey.800" : "grey.100"),
            color: "text.disabled",
          }}
          aria-hidden="true"
        >
          {icon}
        </Box>
        <Box>
          <Typography variant="body1" fontWeight={600}>
            {heading}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        </Box>
      </Stack>
    </SectionCard>
  );
}
