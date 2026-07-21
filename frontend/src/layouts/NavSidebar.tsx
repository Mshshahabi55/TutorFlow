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
import FamilyRestroomRoundedIcon from "@mui/icons-material/FamilyRestroomRounded";
import LinkRoundedIcon from "@mui/icons-material/LinkRounded";
import FactCheckRoundedIcon from "@mui/icons-material/FactCheckRounded";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import EventNoteRoundedIcon from "@mui/icons-material/EventNoteRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import AdminPanelSettingsRoundedIcon from "@mui/icons-material/AdminPanelSettingsRounded";
import ListAltRoundedIcon from "@mui/icons-material/ListAltRounded";
import LockResetRoundedIcon from "@mui/icons-material/LockResetRounded";
import { NavLink } from "react-router-dom";
import { Fragment, type ReactNode } from "react";
import { paths } from "@/routes/paths";
import { useCurrentActor } from "@/shared/hooks/useCurrentActor";
import { useAuth } from "@/shared/hooks/useAuth";
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
        icon: <FamilyRestroomRoundedIcon />,
      },
      {
        to: paths.identity.parentGuardianDetailBase,
        label: "Parent/Guardians",
        icon: <FamilyRestroomRoundedIcon />,
      },
      { to: paths.identity.relationships, label: "Relationships", icon: <LinkRoundedIcon /> },
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
        icon: <EventNoteRoundedIcon />,
        roles: ["Student", "ParentGuardian"],
      },
      {
        to: paths.scheduling.tutorScheduleBase,
        label: "Tutor sessions",
        icon: <EventNoteRoundedIcon />,
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

function normalizeActorRole(role: string | undefined): ActorRole | null {
  if (!role) {
    return null;
  }

  if (role === "Student" || role === "Tutor" || role === "ParentGuardian" || role === "AdminStaff") {
    return role;
  }

  return null;
}

function NavList({ onNavigate }: { onNavigate?: () => void }) {
  const { actor } = useCurrentActor();
  const { user, isAuthenticated } = useAuth();
  const authenticatedRole = isAuthenticated ? normalizeActorRole(user?.role) : null;
  const effectiveRole = authenticatedRole ?? actor.role;

  return (
    <List component="nav" aria-label="Primary">
      <ListItemButton component={NavLink} to={paths.home} onClick={onNavigate}>
        <ListItemIcon>
          <DashboardRoundedIcon />
        </ListItemIcon>
        <ListItemText primary="Dashboard" />
      </ListItemButton>

      {SECTIONS.map((section) => (
        <Fragment key={section.title}>
          <ListSubheader component="div">{section.title}</ListSubheader>
          {visibleEntries(section.entries, effectiveRole).map((entry) => (
            <ListItemButton key={entry.to} component={NavLink} to={entry.to} onClick={onNavigate}>
              <ListItemIcon>{entry.icon}</ListItemIcon>
              <ListItemText primary={entry.label} />
            </ListItemButton>
          ))}
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
 * selected dev role for preview purposes. This is never access control:
 * every route remains reachable by direct URL regardless of role.
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
