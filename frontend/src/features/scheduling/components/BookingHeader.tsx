import { Typography } from "@mui/material";
import { PageHeader } from "@/shared/components/PageHeader";

export interface BookingHeaderProps {
  /** True once a Tutor (and/or a selected Availability Slot) is known from the query string. */
  hasContext: boolean;
}

/** Reuses the app's own `PageHeader` — the copy just adapts to whether we already know who/what is being booked. */
export function BookingHeader({ hasContext }: BookingHeaderProps) {
  return (
    <PageHeader
      title="Book Your Lesson"
      subtitle={
        <Typography variant="body1" color="text.secondary">
          {hasContext
            ? "Pick a date and time, then confirm — it only takes a minute."
            : "Choose a tutor to get started."}
        </Typography>
      }
    />
  );
}
