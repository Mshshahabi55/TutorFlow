import { lazy, Suspense, useState } from "react";
import { AppBar, Box, IconButton, Toolbar, Typography, useMediaQuery, useTheme } from "@mui/material";
import MenuRoundedIcon from "@mui/icons-material/MenuRounded";
import { Outlet } from "react-router-dom";
import { AuthStatus } from "@/layouts/AuthStatus";
import { NavSidebar, NAV_SIDEBAR_WIDTH } from "@/layouts/NavSidebar";

/**
 * Dev-only "Acting as" preview control — never meant to ship (Phase 4.9
 * Task 2; the same gating precedent as Phase D1's StyleGuidePage). Vite's
 * `define` transform replaces `import.meta.env.DEV` with the literal
 * `false` before Rollup bundles a production build, collapsing this whole
 * ternary — dynamic `import()` included — to dead code that Rollup
 * tree-shakes out entirely: no RoleSwitcher chunk reaches `dist/` (verified
 * the same way Phase D1 verified StyleGuidePage's exclusion).
 */
const RoleSwitcher = import.meta.env.DEV
  ? lazy(() => import("@/layouts/RoleSwitcher").then((module) => ({ default: module.RoleSwitcher })))
  : null;

/**
 * Phase D2: the widest a page's content column ever grows on a desktop
 * viewport — past this, line lengths and card grids get harder to scan,
 * not easier, so content stays centered with breathing room either side
 * instead of sprawling edge-to-edge (Task 1 audit: no such limit existed
 * before this phase). Deliberately below the `xl` breakpoint (1536px),
 * not tied to any single breakpoint value, since it is a reading-width
 * choice, not a layout-collapse one.
 */
const CONTENT_MAX_WIDTH = 1200;

/**
 * The application shell: top bar, primary navigation, and the routed page
 * content. Navigation is a permanent sidebar from the "md" breakpoint up
 * (tablet landscape/desktop) and collapses to a hamburger-triggered overlay
 * drawer below it (mobile/tablet portrait), per Sprint 5's responsive
 * requirement, restyled in Phase D2 to the D1 spacing scale with a bounded
 * content column (see CONTENT_MAX_WIDTH above).
 */
export function AppLayout() {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up("md"));
  const [mobileNavOpen, setMobileNavOpen] = useState(false);

  return (
    <Box display="flex" flexDirection="column" minHeight="100vh">
      <Box display="flex" flex={1}>
        <AppBar
          position="fixed"
          color="default"
          sx={{
            zIndex: (t) => t.zIndex.drawer + 1,
            borderBottom: 1,
            borderColor: "divider",
          }}
        >
          <Toolbar sx={{ gap: { xs: 0.5, sm: 2 }, px: { xs: 1.5, sm: 2 } }}>
            {!isDesktop ? (
              <IconButton
                edge="start"
                aria-label="Open navigation"
                onClick={() => setMobileNavOpen(true)}
              >
                <MenuRoundedIcon />
              </IconButton>
            ) : null}
            <Typography
              variant="h6"
              component="div"
              noWrap
              sx={{ flexGrow: 1, fontWeight: 700, minWidth: 0 }}
            >
              TutorFlow
            </Typography>
            <Box display="flex" alignItems="center" gap={{ xs: 1, sm: 1.5 }} flexShrink={0}>
              <AuthStatus />
              {RoleSwitcher ? (
                <Suspense fallback={null}>
                  <RoleSwitcher />
                </Suspense>
              ) : null}
            </Box>
          </Toolbar>
        </AppBar>
        <NavSidebar
          variant={isDesktop ? "permanent" : "temporary"}
          open={isDesktop ? true : mobileNavOpen}
          onClose={() => setMobileNavOpen(false)}
        />
        <Box
          component="main"
          flex={1}
          minWidth={0}
          sx={{ ml: isDesktop ? `${NAV_SIDEBAR_WIDTH}px` : 0 }}
        >
          <Toolbar />
          <Box
            sx={{
              maxWidth: CONTENT_MAX_WIDTH,
              mx: "auto",
              px: { xs: 2, sm: 3, md: 4 },
              py: { xs: 3, sm: 4 },
            }}
          >
            <Outlet />
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
