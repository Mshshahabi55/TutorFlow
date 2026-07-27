import { ListItemButton, ListItemIcon, ListItemText, Tooltip } from "@mui/material";
import { NavLink, useLocation } from "react-router-dom";
import type { ReactNode } from "react";

export interface NavigationItemProps {
  to: string;
  label: string;
  icon: ReactNode;
  onNavigate?: () => void;
  /** Exact-match only (e.g. Dashboard, "/") — every other entry also matches its own sub-routes. */
  exact?: boolean;
  /** Icon-only rail mode (Phase D4): label moves into a Tooltip instead of rendering inline. */
  collapsed?: boolean;
}

/**
 * Phase D4: the one sidebar link component — previously the Dashboard
 * link and every section entry each wrote their own near-identical
 * `ListItemButton` (Task 1 audit: real duplication, not just similar
 * code). The same boolean still drives both the visual `.Mui-selected`
 * indicator (Phase D1) and `aria-current="page"` (Phase D2 Task 3) — a
 * screen-reader user gets the identical answer a sighted user gets from
 * the accent bar, never a state where the two could disagree.
 */
export function NavigationItem({ to, label, icon, onNavigate, exact, collapsed }: NavigationItemProps) {
  const { pathname } = useLocation();
  const isSelected = exact ? pathname === to : pathname === to || pathname.startsWith(`${to}/`);

  const button = (
    <ListItemButton
      component={NavLink}
      to={to}
      end={exact}
      onClick={onNavigate}
      selected={isSelected}
      aria-current={isSelected ? "page" : undefined}
      sx={collapsed ? { justifyContent: "center", px: 1.5 } : undefined}
    >
      <ListItemIcon sx={collapsed ? { minWidth: 0 } : undefined}>{icon}</ListItemIcon>
      {collapsed ? null : <ListItemText primary={label} />}
    </ListItemButton>
  );

  return collapsed ? (
    <Tooltip title={label} placement="right">
      {button}
    </Tooltip>
  ) : (
    button
  );
}
