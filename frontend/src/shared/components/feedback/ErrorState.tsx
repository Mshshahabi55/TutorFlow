import { Alert, AlertTitle, Box, Button } from "@mui/material";
import { ApiRequestError } from "@/services/api/ApiRequestError";

export interface ErrorStateProps {
  error: unknown;
  onRetry?: () => void;
  title?: string;
}

function describeError(error: unknown): string {
  if (error instanceof ApiRequestError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Something went wrong. Please try again.";
}

/**
 * A consistent error placeholder for any query-backed view. Distinguishes
 * a Domain Error (a specific, business-meaningful message, per ADR-008)
 * from an Infrastructure Failure only by the message the backend already
 * produced — this component invents no new classification of its own.
 */
export function ErrorState({ error, onRetry, title = "Something went wrong" }: ErrorStateProps) {
  return (
    <Box py={2}>
      <Alert
        severity="error"
        action={
          onRetry ? (
            <Button color="inherit" size="small" onClick={onRetry}>
              Try again
            </Button>
          ) : undefined
        }
      >
        <AlertTitle>{title}</AlertTitle>
        {describeError(error)}
      </Alert>
    </Box>
  );
}
