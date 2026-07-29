/* eslint-disable react-refresh/only-export-components -- this module exports nav data (icons embedded as JSX) and a lookup function, not a component; there is no component state for Fast Refresh to preserve here. */
import HomeRoundedIcon from "@mui/icons-material/HomeRounded";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import FavoriteRoundedIcon from "@mui/icons-material/FavoriteRounded";
import CalendarMonthRoundedIcon from "@mui/icons-material/CalendarMonthRounded";
import ChatBubbleOutlineRoundedIcon from "@mui/icons-material/ChatBubbleOutlineRounded";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import DashboardRoundedIcon from "@mui/icons-material/DashboardRounded";
import GroupsRoundedIcon from "@mui/icons-material/GroupsRounded";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import FamilyRestroomRoundedIcon from "@mui/icons-material/FamilyRestroomRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import AdminPanelSettingsRoundedIcon from "@mui/icons-material/AdminPanelSettingsRounded";
import FactCheckRoundedIcon from "@mui/icons-material/FactCheckRounded";
import ListAltRoundedIcon from "@mui/icons-material/ListAltRounded";
import LockResetRoundedIcon from "@mui/icons-material/LockResetRounded";
import type { ReactNode } from "react";
import { paths } from "@/routes/paths";
import type { ActorRole } from "@/shared/context/ActorContext";

export interface NavEntry {
  to: string;
  label: string;
  icon: ReactNode;
  exact?: boolean;
}

/**
 * RC2: replaces the old shared `SECTIONS` array — every role's nav
 * grouped under bounded-context section titles ("Identity &
 * Relationship", "Scheduling & Booking", "Marketplace Oversight") that
 * read as an internal admin panel's information architecture, not a
 * consumer marketplace's (Preply/Italki/Cambly). Each role now gets its
 * own short, flat list — no section headers, no jargon. Filtering by role
 * remains UX only, not the access-control boundary (ADR-011): the routes
 * behind these links still carry their own `RequireRole` guard in
 * `router.tsx` where the backend actually restricts them.
 */
export const STUDENT_NAV: NavEntry[] = [
  { to: paths.home, label: "Home", icon: <HomeRoundedIcon />, exact: true },
  { to: paths.discovery.tutorSearch, label: "Find Tutors", icon: <SearchRoundedIcon /> },
  { to: paths.discovery.favorites, label: "Favorites", icon: <FavoriteRoundedIcon /> },
  { to: paths.scheduling.studentScheduleBase, label: "My Lessons", icon: <CalendarMonthRoundedIcon /> },
  { to: paths.messages.inbox, label: "Messages", icon: <ChatBubbleOutlineRoundedIcon /> },
  { to: paths.profile, label: "Profile", icon: <PersonRoundedIcon /> },
];

export const TUTOR_NAV: NavEntry[] = [
  { to: paths.home, label: "Dashboard", icon: <DashboardRoundedIcon />, exact: true },
  { to: paths.scheduling.tutorStudents, label: "My Students", icon: <GroupsRoundedIcon /> },
  { to: paths.scheduling.tutorScheduleBase, label: "My Lessons", icon: <CalendarMonthRoundedIcon /> },
  { to: paths.scheduling.declareAvailability, label: "Availability", icon: <EventAvailableRoundedIcon /> },
  { to: paths.messages.inbox, label: "Messages", icon: <ChatBubbleOutlineRoundedIcon /> },
  { to: paths.profile, label: "Profile", icon: <PersonRoundedIcon /> },
];

export const PARENT_NAV: NavEntry[] = [
  { to: paths.home, label: "Home", icon: <HomeRoundedIcon />, exact: true },
  { to: paths.identity.relationships, label: "My Children", icon: <FamilyRestroomRoundedIcon /> },
  { to: paths.scheduling.bookSession, label: "Book a Lesson", icon: <EventRoundedIcon /> },
  { to: paths.discovery.favorites, label: "Favorites", icon: <FavoriteRoundedIcon /> },
  { to: paths.messages.inbox, label: "Messages", icon: <ChatBubbleOutlineRoundedIcon /> },
  { to: paths.profile, label: "Profile", icon: <PersonRoundedIcon /> },
];

/** Admin navigation remains its own, separate, operations-flavoured list — RC2 explicitly scopes the marketplace redesign to the Student/Tutor/Parent-facing experience. RC5.1 adds "Support Messages" — Admin/Staff <-> any user, per ADR-022. */
export const ADMIN_NAV: NavEntry[] = [
  { to: paths.home, label: "Dashboard", icon: <DashboardRoundedIcon />, exact: true },
  {
    to: paths.oversight.adminDashboard,
    label: "Operations",
    icon: <AdminPanelSettingsRoundedIcon />,
  },
  { to: paths.identity.tutorPending, label: "Tutor Approvals", icon: <FactCheckRoundedIcon /> },
  { to: paths.oversight.globalSessions, label: "All Sessions", icon: <ListAltRoundedIcon /> },
  { to: paths.messages.inbox, label: "Support Messages", icon: <ChatBubbleOutlineRoundedIcon /> },
  { to: paths.auth.resetPassword, label: "Reset Password", icon: <LockResetRoundedIcon /> },
];

/** No role selected yet (dev preview default) — just enough to explore the marketplace read-only. */
export const NO_ROLE_NAV: NavEntry[] = [
  { to: paths.home, label: "Home", icon: <HomeRoundedIcon />, exact: true },
  { to: paths.discovery.tutorSearch, label: "Find Tutors", icon: <SearchRoundedIcon /> },
];

export function navForRole(role: ActorRole | null): NavEntry[] {
  switch (role) {
    case "Student":
      return STUDENT_NAV;
    case "Tutor":
      return TUTOR_NAV;
    case "ParentGuardian":
      return PARENT_NAV;
    case "AdminStaff":
      return ADMIN_NAV;
    default:
      return NO_ROLE_NAV;
  }
}

/** Every entry across every role's nav, deduped by path — used only to resolve a breadcrumb label for the current route, regardless of which role's list it came from. */
export const ALL_NAV_ENTRIES: NavEntry[] = Array.from(
  new Map(
    [...STUDENT_NAV, ...TUTOR_NAV, ...PARENT_NAV, ...ADMIN_NAV, ...NO_ROLE_NAV].map((entry) => [
      entry.to,
      entry,
    ]),
  ).values(),
);
