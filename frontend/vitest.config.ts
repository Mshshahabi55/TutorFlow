import { defineConfig, mergeConfig } from "vitest/config";
import viteConfig from "./vite.config";

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: "jsdom",
      globals: true,
      setupFiles: ["./src/test/setupTests.ts"],
      css: true,
      // Restores every vi.spyOn/vi.fn to its original implementation and
      // clears call history before each test. Without this, a `vi.spyOn`
      // on the same module export across two `it()` blocks in one file
      // returns the *same* mock instance, so a later test's assertion
      // about "not called" sees an earlier test's leftover call history —
      // surfaced by the vitest 2 -> 4 upgrade (Phase 3.5 Task 4), not
      // introduced by it: this was a latent test-isolation gap.
      restoreMocks: true,
      // Default (5000ms) left too little headroom for userEvent-driven MUI
      // component tests when run immediately after other heavy steps (e.g.
      // scripts/verify.ps1's backend suite + a fresh npm ci) on a loaded
      // machine — see docs/phases/PHASE-01C-REPORT.md Section 4.
      testTimeout: 15000,
    },
  }),
);
