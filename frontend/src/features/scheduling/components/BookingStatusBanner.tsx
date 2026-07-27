import { Link as RouterLink } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import { CopyableId } from "@/shared/components/CopyableId";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { paths } from "@/routes/paths";
import type { SessionDto } from "@/services/api/dtos";

export type BookingStatusBannerProps =
  | { status: "success"; session: SessionDto }
  | { status: "error"; error: unknown; onRetry?: () => void };

/** The post-submit result of `POST /sessions` (`useBookSession`) — same mutation, just a clearer success/failure presentation than the old inline text. */
export function BookingStatusBanner(props: BookingStatusBannerProps) {
  if (props.status === "error") {
    return (
      <Card variant="outlined" sx={{ borderColor: "error.main" }}>
        <CardContent>
          <ErrorState error={props.error} onRetry={props.onRetry} title="Booking failed" />
        </CardContent>
      </Card>
    );
  }

  const { session } = props;

  return (
    <Card variant="outlined" sx={{ borderColor: "success.main" }}>
      <CardContent>
        <Stack spacing={2} alignItems="flex-start">
          <Stack direction="row" spacing={1} alignItems="center">
            <CheckCircleRoundedIcon color="success" aria-hidden="true" />
            <Typography variant="h5" component="h2" fontWeight={600}>
              Session booked
            </Typography>
          </Stack>
          <CopyableId id={session.sessionId} />
          <Typography variant="body2" color="text.secondary">
            {toTehranDisplay(session.scheduledTimeUtc)} – {toTehranDisplay(session.endTimeUtc)} (Tehran)
          </Typography>
          <Button
            component={RouterLink}
            to={paths.scheduling.sessionDetail(session.sessionId)}
            variant="contained"
          >
            View this session
          </Button>
        </Stack>
      </CardContent>
    </Card>
  );
}
