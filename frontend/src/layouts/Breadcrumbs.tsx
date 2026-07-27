import { Breadcrumbs as MuiBreadcrumbs, Link as MuiLink, Typography } from "@mui/material";
import { Link as RouterLink, useLocation } from "react-router-dom";
import { ALL_NAV_ENTRIES, navForRole } from "@/layouts/navSections";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import { paths } from "@/routes/paths";

/** Phase D4 / RC2: matches the current route against the same nav entries the sidebar renders — no second copy of route→label data. */
function resolveLabel(pathname: string): string | null {
  for (const entry of ALL_NAV_ENTRIES) {
    if (entry.to === paths.home) {
      continue;
    }
    if (pathname === entry.to || pathname.startsWith(`${entry.to}/`)) {
      return entry.label;
    }
  }

  return null;
}

/**
 * A compact nav trail, not a second copy of the page's own title —
 * `PageHeader` (in every page's content) remains the one place a page's
 * full title renders. RC2 dropped the old bounded-context section titles
 * ("Identity & Relationship", "Scheduling & Booking", ...) along with the
 * nav sections they came from — a flat "Home > Page" trail matches the
 * flat marketplace IA. Falls back to just the home crumb for a route the
 * nav doesn't name (e.g. NotFoundPage, or a detail page reached by id)
 * rather than inventing a label from the URL's own segments.
 */
export function Breadcrumbs() {
  const { pathname } = useLocation();
  const role = useEffectiveRole();
  const homeLabel = navForRole(role)[0]?.label ?? "Dashboard";
  const isHome = pathname === paths.home;
  const label = isHome ? null : resolveLabel(pathname);

  return (
    <MuiBreadcrumbs aria-label="Breadcrumb" sx={{ minWidth: 0 }}>
      {isHome ? (
        <Typography color="text.primary" fontWeight={600} noWrap>
          {homeLabel}
        </Typography>
      ) : (
        <MuiLink component={RouterLink} to={paths.home} underline="hover" color="inherit">
          {homeLabel}
        </MuiLink>
      )}
      {label ? (
        <Typography color="text.primary" fontWeight={600} noWrap>
          {label}
        </Typography>
      ) : null}
    </MuiBreadcrumbs>
  );
}
