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
    port: 5173,
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
