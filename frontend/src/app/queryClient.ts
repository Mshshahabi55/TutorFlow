import { QueryClient } from "@tanstack/react-query";

// Global defaults, established once here so every later feature's hooks
// inherit the same behavior rather than each re-deciding it (Frontend
// Execution Plan, Phase 1). These defaults were chosen for a small API with
// no established business-volume target (ARCHITECTURE.md Section 14), not
// re-tuned since real authentication (docs/adr/ADR-017-authentication-mechanism-decision.md)
// shipped — that changed who calls the API, not the traffic volume this
// tuning is about.
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: 0,
    },
  },
});
