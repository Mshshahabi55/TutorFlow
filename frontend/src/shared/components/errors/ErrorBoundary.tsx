import { Component, type ErrorInfo, type ReactNode } from "react";
import { Box, Button, Container, Typography } from "@mui/material";

export interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: (error: Error, reset: () => void) => ReactNode;
}

interface ErrorBoundaryState {
  error: Error | null;
}

/**
 * A last-resort UI safety net for a rendering error that escapes every
 * query/mutation error state (React error boundaries can only be class
 * components — there is no hook equivalent). Never used as a substitute for
 * handling a known ApiRequestError via ErrorState.
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  override state: ErrorBoundaryState = { error: null };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error };
  }

  override componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error("Unhandled rendering error:", error, info.componentStack);
  }

  private reset = () => {
    this.setState({ error: null });
  };

  override render(): ReactNode {
    const { error } = this.state;

    if (!error) {
      return this.props.children;
    }

    if (this.props.fallback) {
      return this.props.fallback(error, this.reset);
    }

    return (
      <Container maxWidth="sm">
        <Box display="flex" flexDirection="column" alignItems="center" gap={2} py={8} textAlign="center">
          <Typography variant="h5" fontWeight={700}>
            Something went wrong
          </Typography>
          <Typography variant="body2" color="text.secondary">
            The page ran into an unexpected error. You can try reloading this section.
          </Typography>
          <Button variant="contained" onClick={this.reset}>
            Try again
          </Button>
        </Box>
      </Container>
    );
  }
}
