import { lazy, Suspense } from "react";
import { Link as RouterLink } from "react-router-dom";
import { AppBar, Box, Button, IconButton, Toolbar } from "@mui/material";
import MenuRoundedIcon from "@mui/icons-material/MenuRounded";
import { Breadcrumbs } from "@/layouts/Breadcrumbs";
import { SearchFieldPlaceholder } from "@/layouts/SearchFieldPlaceholder";
import { NotificationsButton } from "@/layouts/NotificationsButton";
import { UserMenu } from "@/layouts/UserMenu";
import { ThemeToggle } from "@/shared/components/ThemeToggle";
import { useAuth } from "@/shared/hooks/useAuth";
import { paths } from "@/routes/paths";

/**
 * Dev-only "Acting as" preview control — never meant to ship (Phase 4.9;
 * same gating precedent as Phase D1's StyleGuidePage). Moved here
 * unchanged from `AppLayout.tsx` as part of the header extraction.
 */
const RoleSwitcher = import.meta.env.DEV
  ? lazy(() => import("@/layouts/RoleSwitcher").then((module) => ({ default: module.RoleSwitcher })))
  : null;

export interface AppHeaderProps {
  isDesktop: boolean;
  onOpenMobileNav: () => void;
}

/**
 * Phase D4: extracted from `AppLayout`'s own inline `AppBar`/`Toolbar`
 * (Phase D2/D2-continuation). Stays a single row — no second line — so
 * the two existing `<Toolbar />` spacer usages elsewhere keep working
 * unchanged. The "TutorFlow" wordmark itself moved to `NavSidebar`'s own
 * new header row; this bar's left side is `Breadcrumbs` instead.
 */
export function AppHeader({ isDesktop, onOpenMobileNav }: AppHeaderProps) {
  const { isAuthenticated } = useAuth();

  return (
    <AppBar
      position="fixed"
      color="default"
      sx={{
        zIndex: (t) => t.zIndex.drawer + 1,
        borderBottom: 1,
        borderColor: "divider",
      }}
    >
      <Toolbar sx={{ gap: { xs: 1, sm: 2 }, px: { xs: 2, sm: 3 } }}>
        {!isDesktop ? (
          <IconButton edge="start" aria-label="Open navigation" onClick={onOpenMobileNav}>
            <MenuRoundedIcon />
          </IconButton>
        ) : null}

        <Box sx={{ flexGrow: 1, minWidth: 0 }}>
          <Breadcrumbs />
        </Box>

        <Box sx={{ display: { xs: "none", sm: "block" } }}>
          <SearchFieldPlaceholder />
        </Box>

        <NotificationsButton />
        <ThemeToggle />
        {RoleSwitcher ? (
          <Suspense fallback={null}>
            <RoleSwitcher />
          </Suspense>
        ) : null}

        {isAuthenticated ? (
          <UserMenu />
        ) : (
          <Button component={RouterLink} to={paths.auth.login} variant="outlined">
            Sign in
          </Button>
        )}
      </Toolbar>
    </AppBar>
  );
}
