import { useId, type ReactNode } from "react";
import { Card, CardContent, Stack, Typography } from "@mui/material";

export interface DashboardSectionCardProps {
  title: string;
  /** An optional link/button rendered top-right of the section (e.g. "Browse all"). */
  action?: ReactNode;
  children: ReactNode;
}

/**
 * The one shared container every Student Dashboard section (Upcoming
 * Sessions, Continue Learning, Recommended Tutors, Recent Activity, Quick
 * actions) renders through, so the page reads as one consistent card
 * hierarchy instead of each section inventing its own layout. Rendered as a
 * `<section>` labelled by its own heading so screen-reader users can jump
 * between dashboard sections the same way sighted users scan between cards.
 */
export function DashboardSectionCard({ title, action, children }: DashboardSectionCardProps) {
  const headingId = useId();

  return (
    <Card variant="outlined" component="section" aria-labelledby={headingId} sx={{ height: "100%" }}>
      <CardContent>
        <Stack
          direction="row"
          alignItems="center"
          justifyContent="space-between"
          flexWrap="wrap"
          gap={1}
          mb={2}
        >
          <Typography variant="h5" id={headingId} fontWeight={600}>
            {title}
          </Typography>
          {action}
        </Stack>
        {children}
      </CardContent>
    </Card>
  );
}
