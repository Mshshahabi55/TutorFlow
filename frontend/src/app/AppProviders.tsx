import { CssBaseline, ThemeProvider } from "@mui/material";
import { QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { theme, darkTheme } from "@/app/theme";
import { queryClient } from "@/app/queryClient";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { ColorModeProvider } from "@/shared/context/ColorModeProvider";
import { useColorMode } from "@/shared/hooks/useColorMode";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import { ErrorBoundary } from "@/shared/components/errors/ErrorBoundary";

/**
 * Phase D4: picks `theme`/`darkTheme` (Phase D3) from `useColorMode()`'s
 * resolved mode — split out from `AppProviders` only because a component
 * needs to sit *inside* `ColorModeProvider` to call the hook, while
 * `ThemeProvider` needs to sit *above* everything else in the tree.
 */
function ThemedApp({ children }: { children: ReactNode }) {
  const { resolvedMode } = useColorMode();

  return (
    <ThemeProvider theme={resolvedMode === "dark" ? darkTheme : theme}>
      <CssBaseline />
      {children}
    </ThemeProvider>
  );
}

/** Every cross-cutting provider the app needs, composed in one place so main.tsx/App.tsx stay trivial. */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ErrorBoundary>
      <ColorModeProvider>
        <ThemedApp>
          <QueryClientProvider client={queryClient}>
            <AuthProvider>
              <ActorProvider>
                <NotificationProvider>
                  <ConfirmDialogProvider>{children}</ConfirmDialogProvider>
                </NotificationProvider>
              </ActorProvider>
            </AuthProvider>
          </QueryClientProvider>
        </ThemedApp>
      </ColorModeProvider>
    </ErrorBoundary>
  );
}
