import { QueryClient } from "@tanstack/react-query";

// Global defaults, established once here so every later feature's hooks
// inherit the same behavior rather than each re-deciding it (Frontend
// Execution Plan, Phase 1). No API call in this codebase is authenticated
// yet (ADR-011 remains frozen), so retry/staleTime defaults are chosen for
// a small, currently-anonymous API — not for any specific business volume,
// which no approved document establishes (ARCHITECTURE.md Section 14).
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
