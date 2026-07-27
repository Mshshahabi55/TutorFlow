import { useId, type ReactNode } from "react";
import { Card, CardContent, Stack, Typography } from "@mui/material";

export interface BookingSectionCardProps {
  title: string;
  action?: ReactNode;
  children: ReactNode;
}

/**
 * The Booking page's own section container — same shape as the Student
 * Dashboard's `DashboardSectionCard` (Phase 3 Step 1) and the Tutor
 * Profile's `ProfileSectionCard` (Phase 3 Step 3), kept as a small,
 * separate copy here rather than imported across feature folders (see
 * `shared/validation/guid.ts`'s own comment on why this app never imports
 * one feature's components into another). Rendered as an `<h2>` — this
 * page's own title (`BookingHeader`) is an `<h4>` `PageHeader`, matching
 * every other page in the app.
 */
export function BookingSectionCard({ title, action, children }: BookingSectionCardProps) {
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
          <Typography variant="h5" component="h2" id={headingId} fontWeight={600}>
            {title}
          </Typography>
          {action}
        </Stack>
        {children}
      </CardContent>
    </Card>
  );
}
