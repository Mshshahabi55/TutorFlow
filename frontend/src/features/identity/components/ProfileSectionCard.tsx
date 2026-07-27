import { useId, type ReactNode } from "react";
import { Card, CardContent, Stack, Typography } from "@mui/material";

export interface ProfileSectionCardProps {
  title: string;
  action?: ReactNode;
  children: ReactNode;
}

/**
 * The Tutor Profile's own section container — same shape as the Student
 * Dashboard's `DashboardSectionCard` (Phase 3 Step 1), kept as a small,
 * separate copy here rather than imported across feature folders: this
 * phase is scoped to the Tutor Profile only and must not touch anything
 * under `routes/dashboard/`. Rendered as an `<h2>` (this page's own `<h1>`
 * is `TutorProfileHero`'s tutor name), labelling an ARIA `region` so
 * screen-reader users can jump between profile sections the same way
 * sighted users scan cards.
 */
export function ProfileSectionCard({ title, action, children }: ProfileSectionCardProps) {
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
