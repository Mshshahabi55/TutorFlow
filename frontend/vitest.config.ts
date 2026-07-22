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
      // Default (5000ms) left too little headroom for userEvent-driven MUI
      // component tests when run immediately after other heavy steps (e.g.
      // scripts/verify.ps1's backend suite + a fresh npm ci) on a loaded
      // machine — see docs/phases/PHASE-01C-REPORT.md Section 4.
      testTimeout: 15000,
    },
  }),
);
