import { CssBaseline, ThemeProvider } from "@mui/material";
import { QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { theme } from "@/app/theme";
import { queryClient } from "@/app/queryClient";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import { ErrorBoundary } from "@/shared/components/errors/ErrorBoundary";

/** Every cross-cutting provider the app needs, composed in one place so main.tsx/App.tsx stay trivial. */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ErrorBoundary>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <QueryClientProvider client={queryClient}>
          <AuthProvider>
            <ActorProvider>
              <NotificationProvider>
                <ConfirmDialogProvider>{children}</ConfirmDialogProvider>
              </NotificationProvider>
            </ActorProvider>
          </AuthProvider>
        </QueryClientProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
}
