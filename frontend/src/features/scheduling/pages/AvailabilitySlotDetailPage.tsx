import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useAvailabilitySlot } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { PageHeader } from "@/shared/components/PageHeader";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { DeliveryMode } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

/**
 * No capability exists to list or browse a Tutor's open Availability Slots
 * (see the Sprint 7 Completion Report) — this page is the honest
 * substitute: look a slot up by the id its declaring Tutor shared, then
 * view its detail. The DTO also does not expose whether the slot has
 * already been consumed by a booking, so that state is not claimed here.
 */
export function AvailabilitySlotDetailPage() {
  const { availabilitySlotId } = useParams<{ availabilitySlotId: string }>();
  const navigate = useNavigate();
  const slotQuery = useAvailabilitySlot(availabilitySlotId);

  return (
    <Stack spacing={3} maxWidth={560}>
      <PageHeader
        title="Availability Slot detail"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            There is no way to browse open slots — enter the id shared by the declaring Tutor.
          </Typography>
        }
      />

      {!availabilitySlotId ? (
        <IdLookupForm
          label="Availability Slot id"
          onSubmit={(id) => {
            void navigate(paths.scheduling.availabilitySlotDetail(id));
          }}
        />
      ) : (
        <Card variant="outlined">
          <CardContent>
            {slotQuery.isPending ? <LoadingState label="Loading Availability Slot…" /> : null}
            {slotQuery.isError ? (
              <ErrorState error={slotQuery.error} onRetry={() => void slotQuery.refetch()} />
            ) : null}
            {slotQuery.isSuccess ? (
              <Stack spacing={2}>
                <Stack direction="row" spacing={1} flexWrap="wrap">
                  <StatusPill
                    label={
                      slotQuery.data.deliveryMode === DeliveryMode.Online ? "Online" : "In-Person"
                    }
                    tone="info"
                  />
                </Stack>
                <Stack spacing={1}>
                  <Typography variant="body2">
                    <b>Tutor id:</b> {slotQuery.data.tutorId}
                  </Typography>
                  <Typography variant="body2">
                    <b>Start (Tehran):</b> {toTehranDisplay(slotQuery.data.startTimeUtc)}
                  </Typography>
                  <Typography variant="body2">
                    <b>End (Tehran):</b> {toTehranDisplay(slotQuery.data.endTimeUtc)}
                  </Typography>
                  <Typography variant="body2">
                    <b>Duration:</b> {timeSpanToMinutes(slotQuery.data.duration)} minutes
                  </Typography>
                </Stack>
                <Button
                  component={RouterLink}
                  to={`${paths.scheduling.bookSession}?availabilitySlotId=${slotQuery.data.availabilitySlotId}`}
                  variant="contained"
                  sx={{ alignSelf: "flex-start" }}
                >
                  Book this slot
                </Button>
              </Stack>
            ) : null}
          </CardContent>
        </Card>
      )}
    </Stack>
  );
}
