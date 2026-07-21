import { useState } from "react";
import { AppBar, Box, IconButton, Toolbar, Typography, useMediaQuery, useTheme } from "@mui/material";
import MenuRoundedIcon from "@mui/icons-material/MenuRounded";
import { Outlet } from "react-router-dom";
import { AuthStatus } from "@/layouts/AuthStatus";
import { NavSidebar, NAV_SIDEBAR_WIDTH } from "@/layouts/NavSidebar";
import { RoleSwitcher } from "@/layouts/RoleSwitcher";

/**
 * The application shell: top bar, primary navigation, and the routed page
 * content. Navigation is a permanent sidebar from the "md" breakpoint up
 * (tablet landscape/desktop) and collapses to a hamburger-triggered overlay
 * drawer below it (mobile/tablet portrait), per Sprint 5's responsive
 * requirement.
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
          <Toolbar sx={{ gap: { xs: 1, sm: 2 } }}>
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
            <AuthStatus />
            <RoleSwitcher />
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
          <Box p={{ xs: 2, sm: 3, md: 4 }}>
            <Outlet />
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
