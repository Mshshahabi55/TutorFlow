import { Box, Drawer, IconButton, List, Toolbar, Typography } from "@mui/material";
import ChevronLeftRoundedIcon from "@mui/icons-material/ChevronLeftRounded";
import ChevronRightRoundedIcon from "@mui/icons-material/ChevronRightRounded";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import { NavigationItem } from "@/layouts/NavigationItem";
import { navForRole } from "@/layouts/navSections";

export const NAV_SIDEBAR_WIDTH = 260;
export const NAV_SIDEBAR_WIDTH_COLLAPSED = 72;

/**
 * RC2: one short, flat list per role (`navForRole`) instead of a shared,
 * section-grouped list filtered down — a real consumer marketplace's nav
 * doesn't group by internal bounded context.
 */
function NavList({ onNavigate, collapsed }: { onNavigate?: () => void; collapsed?: boolean }) {
  const effectiveRole = useEffectiveRole();

  return (
    <List component="nav" aria-label="Primary" sx={{ px: 1.5, py: 1 }}>
      {navForRole(effectiveRole).map((entry) => (
        <NavigationItem
          key={entry.to}
          to={entry.to}
          label={entry.label}
          icon={entry.icon}
          onNavigate={onNavigate}
          exact={entry.exact}
          collapsed={collapsed}
        />
      ))}
    </List>
  );
}

export interface NavSidebarProps {
  variant: "permanent" | "temporary";
  open: boolean;
  onClose: () => void;
  /** Icon-rail mode (Phase D4) — meaningful only when `variant === "permanent"`; the mobile overlay drawer is never collapsed. */
  collapsed?: boolean;
  onToggleCollapse?: () => void;
}

/**
 * Renders as a permanent, always-visible drawer on desktop and a temporary,
 * overlay drawer (toggled from AppLayout's hamburger button) on tablet/
 * mobile — the same NavList content either way. Entries are filtered by the
 * authenticated user role when available, and otherwise by the locally-
 * selected dev role for preview purposes. This filtering is UX only, not the
 * access-control boundary itself: some of these routes (the ones whose
 * backend permission grants only specific roles) are additionally wrapped in
 * `RequireRole` at the router level (Phase 4.9 Task 4,
 * `frontend/src/routes/router.tsx`), which is what actually blocks their
 * content for a role reaching them by direct URL — this component only
 * decides what to show a link to.
 *
 * Phase D4: gained its own brand/collapse header row (the "TutorFlow"
 * wordmark moved here from AppHeader — the reference apps this design
 * system targets keep brand with the sidebar, not the top bar) and an
 * icon-rail collapsed mode (permanent variant only), animated over the
 * same 150–250ms band the design system's motion guidance sets.
 */
export function NavSidebar({ variant, open, onClose, collapsed = false, onToggleCollapse }: NavSidebarProps) {
  const isTemporary = variant === "temporary";
  const isCollapsedRail = !isTemporary && collapsed;
  const width = isCollapsedRail ? NAV_SIDEBAR_WIDTH_COLLAPSED : NAV_SIDEBAR_WIDTH;

  return (
    <Drawer
      variant={variant}
      open={open}
      onClose={onClose}
      ModalProps={isTemporary ? { keepMounted: true } : undefined}
      sx={{
        width,
        flexShrink: 0,
        transition: (t) => t.transitions.create("width", { duration: 200 }),
        [`& .MuiDrawer-paper`]: {
          width,
          boxSizing: "border-box",
          transition: (t) => t.transitions.create("width", { duration: 200 }),
          overflowX: "hidden",
        },
      }}
    >
      <Toolbar
        sx={{
          display: "flex",
          alignItems: "center",
          justifyContent: isCollapsedRail ? "center" : "space-between",
          px: isCollapsedRail ? 1 : 2,
        }}
      >
        {isCollapsedRail ? null : (
          <Typography variant="h6" fontWeight={700} noWrap>
            TutorFlow
          </Typography>
        )}
        {isTemporary ? null : (
          <IconButton
            aria-label={collapsed ? "Expand navigation" : "Collapse navigation"}
            onClick={onToggleCollapse}
            size="small"
          >
            {collapsed ? <ChevronRightRoundedIcon /> : <ChevronLeftRoundedIcon />}
          </IconButton>
        )}
      </Toolbar>
      <Box sx={{ overflowY: "auto", overflowX: "hidden" }}>
        <NavList onNavigate={isTemporary ? onClose : undefined} collapsed={isCollapsedRail} />
      </Box>
    </Drawer>
  );
}
