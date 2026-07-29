/** Centralized route paths — every Link/NavLink/navigate call imports from here rather than hardcoding a string. */
export const paths = {
  home: "/",
  /** RC5.1: real Conversation/Message capability (docs/adr/ADR-022-communication-and-notifications-architecture.md). */
  messages: {
    inbox: "/messages",
    conversationDetailPattern: "/messages/:conversationId",
    conversationDetail: (conversationId: string) => `/messages/${conversationId}`,
  },
  /** RC2: one "Profile" nav destination for every role — resolves to the role's existing detail page. */
  profile: "/profile",
  auth: {
    login: "/auth/login",
    resetPassword: "/auth/reset-password",
  },
  identity: {
    tutorRegister: "/identity/tutors/register",
    tutorDirectory: "/identity/tutors",
    tutorPending: "/identity/tutors/pending",
    tutorDetailPattern: "/identity/tutors/:tutorId",
    tutorDetail: (tutorId: string) => `/identity/tutors/${tutorId}`,
    tutorEditPattern: "/identity/tutors/:tutorId/edit",
    tutorEdit: (tutorId: string) => `/identity/tutors/${tutorId}/edit`,
    /** ADR-024 (Accepted, 2026-07-28): the 7-step Tutor Onboarding Wizard, reached after registration. */
    tutorOnboardingPattern: "/identity/tutors/:tutorId/onboarding",
    tutorOnboarding: (tutorId: string) => `/identity/tutors/${tutorId}/onboarding`,
    studentRegister: "/identity/students/register",
    studentDetailBase: "/identity/students",
    studentDetailPattern: "/identity/students/:studentId",
    studentDetail: (studentId: string) => `/identity/students/${studentId}`,
    parentGuardianRegister: "/identity/parent-guardians/register",
    parentGuardianDetailBase: "/identity/parent-guardians",
    parentGuardianDetailPattern: "/identity/parent-guardians/:parentGuardianId",
    parentGuardianDetail: (parentGuardianId: string) => `/identity/parent-guardians/${parentGuardianId}`,
    relationships: "/identity/relationships",
  },
  scheduling: {
    declareAvailability: "/scheduling/availability/declare",
    availabilitySlotDetailBase: "/scheduling/availability",
    availabilitySlotDetailPattern: "/scheduling/availability/:availabilitySlotId",
    availabilitySlotDetail: (availabilitySlotId: string) =>
      `/scheduling/availability/${availabilitySlotId}`,
    bookSession: "/scheduling/sessions/book",
    sessionDetailBase: "/scheduling/sessions",
    sessionDetailPattern: "/scheduling/sessions/:sessionId",
    sessionDetail: (sessionId: string) => `/scheduling/sessions/${sessionId}`,
    studentScheduleBase: "/scheduling/students",
    studentSchedulePattern: "/scheduling/students/:studentId/schedule",
    studentSchedule: (studentId: string) => `/scheduling/students/${studentId}/schedule`,
    tutorScheduleBase: "/scheduling/tutors",
    tutorSchedulePattern: "/scheduling/tutors/:tutorId/schedule",
    tutorSchedule: (tutorId: string) => `/scheduling/tutors/${tutorId}/schedule`,
    /** RC2: "My Students" — derives the Tutor's unique Students from their own existing schedule, no new endpoint. */
    tutorStudents: "/scheduling/tutors/students",
  },
  discovery: {
    tutorSearch: "/discovery/tutors/search",
  },
  oversight: {
    adminDashboard: "/oversight/dashboard",
    globalSessions: "/oversight/sessions",
  },
  /** Dev-only, unlinked from navigation, excluded from the production build — see router.tsx. */
  dev: {
    styleGuide: "/dev/style-guide",
  },
} as const;
