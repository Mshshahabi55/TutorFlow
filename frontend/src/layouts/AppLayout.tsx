import { useState } from "react";
import { Box, Toolbar, useMediaQuery, useTheme } from "@mui/material";
import { Outlet } from "react-router-dom";
import { AppHeader } from "@/layouts/AppHeader";
import { NavSidebar, NAV_SIDEBAR_WIDTH, NAV_SIDEBAR_WIDTH_COLLAPSED } from "@/layouts/NavSidebar";
import { PageContainer } from "@/shared/components/PageContainer";

const SIDEBAR_COLLAPSED_STORAGE_KEY = "tutorflow.sidebarCollapsed";

function readStoredCollapsed(): boolean {
  if (typeof window === "undefined") {
    return false;
  }

  return window.localStorage.getItem(SIDEBAR_COLLAPSED_STORAGE_KEY) === "true";
}

/**
 * The application shell: top bar, primary navigation, and the routed page
 * content. Navigation is a permanent sidebar from the "md" breakpoint up
 * (tablet landscape/desktop) and collapses to a hamburger-triggered overlay
 * drawer below it (mobile/tablet portrait), per Sprint 5's responsive
 * requirement, restyled in Phase D2 to the D1 spacing scale with a bounded
 * content column (`PageContainer`).
 *
 * Phase D4: the header (`AppHeader`) and content column (`PageContainer`)
 * are now their own components; the sidebar can additionally collapse to
 * an icon-only rail on desktop (`collapsed`, persisted the same
 * `localStorage`-backed way `tutorflow.devActorRole`/`tutorflow.colorMode`
 * already are) — mobile's overlay drawer is unaffected, since collapsing
 * an overlay isn't meaningful.
 */
export function AppLayout() {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up("md"));
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(readStoredCollapsed);

  function toggleCollapsed() {
    setCollapsed((prev) => {
      const next = !prev;
      window.localStorage.setItem(SIDEBAR_COLLAPSED_STORAGE_KEY, String(next));
      return next;
    });
  }

  const sidebarWidth = isDesktop && collapsed ? NAV_SIDEBAR_WIDTH_COLLAPSED : NAV_SIDEBAR_WIDTH;

  return (
    <Box display="flex" flexDirection="column" minHeight="100vh">
      <Box display="flex" flex={1}>
        <AppHeader isDesktop={isDesktop} onOpenMobileNav={() => setMobileNavOpen(true)} />
        <NavSidebar
          variant={isDesktop ? "permanent" : "temporary"}
          open={isDesktop ? true : mobileNavOpen}
          onClose={() => setMobileNavOpen(false)}
          collapsed={collapsed}
          onToggleCollapse={toggleCollapsed}
        />
        <Box
          component="main"
          flex={1}
          minWidth={0}
          sx={{
            ml: isDesktop ? `${sidebarWidth}px` : 0,
            transition: (t) => t.transitions.create("margin-left", { duration: 200 }),
          }}
        >
          <Toolbar />
          <PageContainer>
            <Outlet />
          </PageContainer>
        </Box>
      </Box>
    </Box>
  );
}
