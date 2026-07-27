import {
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  ListSubheader,
  Toolbar,
} from "@mui/material";
import DashboardRoundedIcon from "@mui/icons-material/DashboardRounded";
import PersonAddRoundedIcon from "@mui/icons-material/PersonAddRounded";
import GroupsRoundedIcon from "@mui/icons-material/GroupsRounded";
import SchoolRoundedIcon from "@mui/icons-material/SchoolRounded";
import FamilyRestroomRoundedIcon from "@mui/icons-material/FamilyRestroomRounded";
import LinkRoundedIcon from "@mui/icons-material/LinkRounded";
import FactCheckRoundedIcon from "@mui/icons-material/FactCheckRounded";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import EventNoteRoundedIcon from "@mui/icons-material/EventNoteRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import CalendarMonthRoundedIcon from "@mui/icons-material/CalendarMonthRounded";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import AdminPanelSettingsRoundedIcon from "@mui/icons-material/AdminPanelSettingsRounded";
import ListAltRoundedIcon from "@mui/icons-material/ListAltRounded";
import LockResetRoundedIcon from "@mui/icons-material/LockResetRounded";
import { NavLink, useLocation } from "react-router-dom";
import { Fragment, type ReactNode } from "react";
import { paths } from "@/routes/paths";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import type { ActorRole } from "@/shared/context/ActorContext";

export const NAV_SIDEBAR_WIDTH = 260;

interface NavEntry {
  to: string;
  label: string;
  icon: ReactNode;
  /** Shown for these dev roles, or always when omitted — display convenience only, never access control (ADR-011). */
  roles?: ActorRole[];
}

interface NavSection {
  title: string;
  entries: NavEntry[];
}

const SECTIONS: NavSection[] = [
  {
    title: "Identity & Relationship",
    entries: [
      { to: paths.identity.tutorDirectory, label: "Tutor directory", icon: <GroupsRoundedIcon /> },
      {
        to: paths.identity.tutorRegister,
        label: "Register as Tutor",
        icon: <PersonAddRoundedIcon />,
        roles: ["Tutor"],
      },
      {
        to: paths.identity.studentRegister,
        label: "Register as Student",
        icon: <PersonAddRoundedIcon />,
        roles: ["Student"],
      },
      {
        to: paths.identity.parentGuardianRegister,
        label: "Register as Parent/Guardian",
        icon: <PersonAddRoundedIcon />,
        roles: ["ParentGuardian"],
      },
      {
        to: paths.identity.studentDetailBase,
        label: "Students",
        icon: <SchoolRoundedIcon />,
        // Fine-grained rule behind /students/{id} (AUTHORIZATION_MATRIX.md
        // Addendum Decision 3): Owner + confirmed-Relationship counterpart +
        // Admin only — a Tutor is never a legitimate party to a Student
        // record (Phase 4.9 Task 4; previously unrestricted, a real gap).
        roles: ["Student", "ParentGuardian", "AdminStaff"],
      },
      {
        to: paths.identity.parentGuardianDetailBase,
        label: "Parent/Guardians",
        icon: <FamilyRestroomRoundedIcon />,
        // Same rule, same Source, mirrored for /parent-guardians/{id}.
        roles: ["Student", "ParentGuardian", "AdminStaff"],
      },
      {
        to: paths.identity.relationships,
        label: "Relationships",
        icon: <LinkRoundedIcon />,
        // InviteRelationship/ConfirmRelationship (RolePermissionCatalog) are
        // granted to Student/ParentGuardian only; the page's own account-
        // lookup half is Owner + Admin (Third Addendum Decision 10) — a
        // Tutor has no permission or party status on any of it.
        roles: ["Student", "ParentGuardian", "AdminStaff"],
      },
      {
        to: paths.identity.tutorPending,
        label: "Pending Tutor approvals",
        icon: <FactCheckRoundedIcon />,
        roles: ["AdminStaff"],
      },
    ],
  },
  {
    title: "Discovery",
    entries: [
      { to: paths.discovery.tutorSearch, label: "Search Tutors", icon: <SearchRoundedIcon /> },
    ],
  },
  {
    title: "Scheduling & Booking",
    entries: [
      {
        to: paths.scheduling.declareAvailability,
        label: "Declare availability",
        icon: <EventAvailableRoundedIcon />,
        roles: ["Tutor"],
      },
      {
        to: paths.scheduling.availabilitySlotDetailBase,
        label: "Availability Slot lookup",
        icon: <EventNoteRoundedIcon />,
      },
      {
        to: paths.scheduling.bookSession,
        label: "Book a session",
        icon: <EventRoundedIcon />,
        roles: ["Student", "ParentGuardian"],
      },
      {
        to: paths.scheduling.sessionDetailBase,
        label: "Session lookup",
        icon: <EventNoteRoundedIcon />,
      },
      {
        to: paths.scheduling.studentScheduleBase,
        label: "Student sessions",
        icon: <CalendarMonthRoundedIcon />,
        roles: ["Student", "ParentGuardian"],
      },
      {
        to: paths.scheduling.tutorScheduleBase,
        label: "Tutor sessions",
        icon: <CalendarMonthRoundedIcon />,
        roles: ["Tutor"],
      },
    ],
  },
  {
    title: "Marketplace Oversight",
    entries: [
      {
        to: paths.oversight.adminDashboard,
        label: "Admin dashboard",
        icon: <AdminPanelSettingsRoundedIcon />,
        roles: ["AdminStaff"],
      },
      {
        to: paths.oversight.globalSessions,
        label: "All sessions",
        icon: <ListAltRoundedIcon />,
        roles: ["AdminStaff"],
      },
      {
        to: paths.auth.resetPassword,
        label: "Reset account password",
        icon: <LockResetRoundedIcon />,
        roles: ["AdminStaff"],
      },
    ],
  },
];

function visibleEntries(entries: NavEntry[], role: ActorRole | null): NavEntry[] {
  return entries.filter((entry) => !entry.roles || role === null || entry.roles.includes(role));
}

function NavList({ onNavigate }: { onNavigate?: () => void }) {
  const effectiveRole = useEffectiveRole();
  const { pathname } = useLocation();

  const dashboardSelected = pathname === paths.home;

  return (
    <List component="nav" aria-label="Primary">
      <ListItemButton
        component={NavLink}
        to={paths.home}
        onClick={onNavigate}
        selected={dashboardSelected}
        aria-current={dashboardSelected ? "page" : undefined}
      >
        <ListItemIcon>
          <DashboardRoundedIcon />
        </ListItemIcon>
        <ListItemText primary="Dashboard" />
      </ListItemButton>

      {SECTIONS.map((section) => (
        <Fragment key={section.title}>
          <ListSubheader component="div">{section.title}</ListSubheader>
          {visibleEntries(section.entries, effectiveRole).map((entry) => {
            // The same boolean drives both the visual indicator (D1's
            // .Mui-selected accent bar) and the accessible "current page"
            // signal — a screen reader user gets exactly the same answer
            // to "where am I" as a sighted one, never a state where the
            // two could disagree (Task 3: "a person always knows where
            // they are").
            const isSelected = pathname === entry.to || pathname.startsWith(`${entry.to}/`);
            return (
              <ListItemButton
                key={entry.to}
                component={NavLink}
                to={entry.to}
                onClick={onNavigate}
                selected={isSelected}
                aria-current={isSelected ? "page" : undefined}
              >
                <ListItemIcon>{entry.icon}</ListItemIcon>
                <ListItemText primary={entry.label} />
              </ListItemButton>
            );
          })}
        </Fragment>
      ))}
    </List>
  );
}

export interface NavSidebarProps {
  variant: "permanent" | "temporary";
  open: boolean;
  onClose: () => void;
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
 */
export function NavSidebar({ variant, open, onClose }: NavSidebarProps) {
  const isTemporary = variant === "temporary";

  return (
    <Drawer
      variant={variant}
      open={open}
      onClose={onClose}
      ModalProps={isTemporary ? { keepMounted: true } : undefined}
      sx={{
        width: NAV_SIDEBAR_WIDTH,
        flexShrink: 0,
        [`& .MuiDrawer-paper`]: { width: NAV_SIDEBAR_WIDTH, boxSizing: "border-box" },
      }}
    >
      <Toolbar />
      <NavList onNavigate={isTemporary ? onClose : undefined} />
    </Drawer>
  );
}
