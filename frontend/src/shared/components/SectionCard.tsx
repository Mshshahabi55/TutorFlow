import { useId, type ElementType, type ReactNode } from "react";
import { Card, CardContent, Stack, Typography } from "@mui/material";

export interface SectionCardProps {
  title: string;
  /** An optional link/button rendered top-right of the section (e.g. "View all"). */
  action?: ReactNode;
  children: ReactNode;
  /** An optional DOM id, e.g. so an in-page anchor nav (TutorDetailPage's section nav) can scroll to this section. */
  id?: string;
  /**
   * The heading's semantic tag. Defaults to `<h5>`, correct directly under
   * this app's usual `<h4>` `PageHeader` (every Dashboard, Booking,
   * Scheduling, and Oversight page). Pass `"h2"` only when the page's own
   * title is a real `<h1>` instead (currently just `TutorDetailPage`'s
   * `TutorProfileHero`).
   */
  headingComponent?: ElementType;
}

/**
 * RC1 hardening: the one section container every workspace page renders
 * through — Student/Tutor/Parent/Admin Dashboards, the Booking flow,
 * Session pages, and the Tutor Profile. Previously three byte-identical
 * copies (`DashboardSectionCard`, `BookingSectionCard`,
 * `ProfileSectionCard`), kept separate across Phase 3's incremental,
 * per-workspace steps to avoid cross-feature imports mid-phase. Now that
 * the redesign phase is complete, merging them removes real duplication
 * with identical responsibility — consolidating also fixed a genuine
 * inconsistency `BookingSectionCard` had introduced: it forced every
 * heading to `<h2>` even on pages whose title is a `<h4>` `PageHeader`,
 * which nests backwards, not correctly. Rendered as a `<section>` labelled
 * by its own heading so screen-reader users can jump between sections the
 * same way sighted users scan cards.
 */
export function SectionCard({ title, action, children, headingComponent = "h5", id }: SectionCardProps) {
  const headingId = useId();

  return (
    <Card
      id={id}
      variant="outlined"
      component="section"
      aria-labelledby={headingId}
      sx={{ height: "100%", scrollMarginTop: 72 }}
    >
      <CardContent>
        <Stack
          direction="row"
          alignItems="center"
          justifyContent="space-between"
          flexWrap="wrap"
          gap={1}
          mb={2}
        >
          <Typography variant="h5" component={headingComponent} id={headingId} fontWeight={600}>
            {title}
          </Typography>
          {action}
        </Stack>
        {children}
      </CardContent>
    </Card>
  );
}
