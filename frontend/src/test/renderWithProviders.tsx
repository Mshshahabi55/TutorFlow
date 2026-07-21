import { render } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import type { ReactElement, ReactNode } from "react";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { ActorProvider } from "@/shared/context/ActorProvider";

export interface RenderWithProvidersOptions {
  /** Defaults to ["/"]. Set to a path matching `routePath` (e.g. "/identity/tutors/abc") to exercise useParams. */
  initialEntries?: string[];
  /** When the page under test reads a route param (useParams), pass its route pattern (e.g. "/identity/tutors/:tutorId"). */
  routePath?: string;
}

/**
 * Shared by every feature-page test: QueryClient (retries off, so failures
 * resolve immediately in tests), MemoryRouter, and the shell providers
 * (Notification, ConfirmDialog, Auth, Actor) pages/mutations depend on.
 * Extracted here once most pages needed the identical provider stack.
 */
export function renderWithProviders(ui: ReactElement, options: RenderWithProvidersOptions = {}) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  const content = options.routePath ? (
    <Routes>
      <Route path={options.routePath} element={ui} />
    </Routes>
  ) : (
    ui
  );

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={options.initialEntries ?? ["/"]}>
          <AuthProvider>
            <ActorProvider>
              <NotificationProvider>
                <ConfirmDialogProvider>{children}</ConfirmDialogProvider>
              </NotificationProvider>
            </ActorProvider>
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    );
  }

  return { queryClient, ...render(content, { wrapper: Wrapper }) };
}
