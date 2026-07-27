import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(import.meta.dirname, "./src"),
    },
  },
  server: {
    // Deterministic dev port: silently drifting to another port (Vite's
    // default fallback when 5173 is already taken) is exactly what broke
    // every form behind CORS in Phase 4.8 — the backend's
    // Cors:AllowedOrigins only ever matched the port that happened to be
    // free, not necessarily 5173. strictPort makes a port conflict a loud
    // startup failure instead of a silent drift.
    port: 5173,
    strictPort: true,
  },
  build: {
    rollupOptions: {
      output: {
        // Every route is already code-split (src/routes/router.tsx); these
        // vendor libraries are required by the app shell on first paint
        // regardless, so splitting them out doesn't reduce first-load bytes.
        // It does let browsers cache them separately from app code, which
        // changes on every deploy while these dependency versions do not.
        manualChunks: {
          "vendor-react": ["react", "react-dom", "react-router-dom"],
          "vendor-mui": ["@mui/material", "@mui/icons-material", "@emotion/react", "@emotion/styled"],
          "vendor-data": ["@tanstack/react-query", "axios", "react-hook-form", "@hookform/resolvers", "zod"],
        },
      },
    },
  },
});
