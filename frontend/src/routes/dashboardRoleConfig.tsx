import type { ReactNode } from "react";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import CalendarMonthRoundedIcon from "@mui/icons-material/CalendarMonthRounded";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import LinkRoundedIcon from "@mui/icons-material/LinkRounded";
import FactCheckRoundedIcon from "@mui/icons-material/FactCheckRounded";
import AdminPanelSettingsRoundedIcon from "@mui/icons-material/AdminPanelSettingsRounded";
import ListAltRoundedIcon from "@mui/icons-material/ListAltRounded";
import type { ActorRole } from "@/shared/context/ActorContext";
import { paths } from "@/routes/paths";

export interface QuickAction {
  label: string;
  to: string;
  icon: ReactNode;
}

/**
 * Phase D2 Task 4: replaces the old static "Available modules" list — four
 * inert, non-interactive pills naming every bounded context, with no link
 * and no distinction between them (the Task 1 audit's clutter finding).
 * Each role instead gets its own short, actually-navigable set of next
 * steps, reusing the exact routes NavSidebar already exposes for that role
 * — no new page, no new capability, just the same destinations surfaced
 * one click sooner from the page a signed-in user lands on first.
 *
 * Extracted from `DashboardPage.tsx` (Phase 3 Step 1) so `StudentDashboard`
 * can reuse the Student entries without duplicating them or creating a
 * circular import between the two page modules.
 */
export const ROLE_QUICK_ACTIONS: Record<ActorRole, QuickAction[]> = {
  Student: [
    { label: "Search Tutors", to: paths.discovery.tutorSearch, icon: <SearchRoundedIcon /> },
    { label: "Book a session", to: paths.scheduling.bookSession, icon: <EventRoundedIcon /> },
    {
      label: "My sessions",
      to: paths.scheduling.studentScheduleBase,
      icon: <CalendarMonthRoundedIcon />,
    },
  ],
  Tutor: [
    {
      label: "Declare availability",
      to: paths.scheduling.declareAvailability,
      icon: <EventAvailableRoundedIcon />,
    },
    {
      label: "My sessions",
      to: paths.scheduling.tutorScheduleBase,
      icon: <CalendarMonthRoundedIcon />,
    },
  ],
  ParentGuardian: [
    { label: "Relationships", to: paths.identity.relationships, icon: <LinkRoundedIcon /> },
    { label: "Book a session", to: paths.scheduling.bookSession, icon: <EventRoundedIcon /> },
    {
      label: "Student sessions",
      to: paths.scheduling.studentScheduleBase,
      icon: <CalendarMonthRoundedIcon />,
    },
  ],
  AdminStaff: [
    {
      label: "Pending Tutor approvals",
      to: paths.identity.tutorPending,
      icon: <FactCheckRoundedIcon />,
    },
    {
      label: "Admin dashboard",
      to: paths.oversight.adminDashboard,
      icon: <AdminPanelSettingsRoundedIcon />,
    },
    { label: "All sessions", to: paths.oversight.globalSessions, icon: <ListAltRoundedIcon /> },
  ],
};
