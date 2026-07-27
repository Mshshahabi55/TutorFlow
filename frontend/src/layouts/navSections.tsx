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
import type { ReactNode } from "react";
import { paths } from "@/routes/paths";
import type { ActorRole } from "@/shared/context/ActorContext";

export interface NavEntry {
  to: string;
  label: string;
  icon: ReactNode;
  /** Shown for these dev roles, or always when omitted — display convenience only, never access control (ADR-011). */
  roles?: ActorRole[];
}

export interface NavSection {
  title: string;
  entries: NavEntry[];
}

/**
 * Phase D4: split out of `NavSidebar.tsx` into its own (non-component)
 * data file — `Breadcrumbs` reuses this same data to derive a route's
 * trail, and `react-refresh/only-export-components` flags a component
 * file that also exports plain data/functions.
 */
export const SECTIONS: NavSection[] = [
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

export function visibleEntries(entries: NavEntry[], role: ActorRole | null): NavEntry[] {
  return entries.filter((entry) => !entry.roles || role === null || entry.roles.includes(role));
}
