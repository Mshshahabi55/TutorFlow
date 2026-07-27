import { Breadcrumbs as MuiBreadcrumbs, Link as MuiLink, Typography } from "@mui/material";
import { Link as RouterLink, useLocation } from "react-router-dom";
import { SECTIONS } from "@/layouts/navSections";
import { paths } from "@/routes/paths";

interface Trail {
  sectionTitle?: string;
  label: string;
}

/** Phase D4: matches the current route against NavSidebar's own SECTIONS — no second copy of route→label data. */
function resolveTrail(pathname: string): Trail | null {
  for (const section of SECTIONS) {
    for (const entry of section.entries) {
      if (pathname === entry.to || pathname.startsWith(`${entry.to}/`)) {
        return { sectionTitle: section.title, label: entry.label };
      }
    }
  }

  return null;
}

/**
 * A compact nav trail, not a second copy of the page's own title —
 * `PageHeader` (in every page's content) remains the one place a page's
 * full title renders. Falls back to just "Dashboard" (home) or the bare
 * brand for a route `SECTIONS` doesn't name (e.g. NotFoundPage) rather
 * than inventing a label from the URL's own segments.
 */
export function Breadcrumbs() {
  const { pathname } = useLocation();
  const isHome = pathname === paths.home;
  const trail = isHome ? null : resolveTrail(pathname);

  return (
    <MuiBreadcrumbs aria-label="Breadcrumb" sx={{ minWidth: 0 }}>
      {isHome ? (
        <Typography color="text.primary" fontWeight={600} noWrap>
          Dashboard
        </Typography>
      ) : (
        <MuiLink component={RouterLink} to={paths.home} underline="hover" color="inherit">
          Dashboard
        </MuiLink>
      )}
      {trail?.sectionTitle ? (
        <Typography color="text.secondary" noWrap>
          {trail.sectionTitle}
        </Typography>
      ) : null}
      {trail ? (
        <Typography color="text.primary" fontWeight={600} noWrap>
          {trail.label}
        </Typography>
      ) : null}
    </MuiBreadcrumbs>
  );
}
