import { lazy, Suspense, type ReactNode } from "react";
import { createBrowserRouter } from "react-router-dom";
import { AppLayout } from "@/layouts/AppLayout";
import { paths } from "@/routes/paths";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { RequireRole } from "@/shared/components/RequireRole";
import type { ActorRole } from "@/shared/context/ActorContext";

const DashboardPage = lazy(() =>
  import("@/routes/DashboardPage").then((module) => ({ default: module.DashboardPage })),
);
const NotFoundPage = lazy(() =>
  import("@/routes/NotFoundPage").then((module) => ({ default: module.NotFoundPage })),
);

const LoginPage = lazy(() =>
  import("@/features/auth/pages/LoginPage").then((module) => ({ default: module.LoginPage })),
);
const AdminResetPasswordPage = lazy(() =>
  import("@/features/auth/pages/AdminResetPasswordPage").then((module) => ({
    default: module.AdminResetPasswordPage,
  })),
);

const RegisterTutorPage = lazy(() =>
  import("@/features/identity/pages/RegisterTutorPage").then((module) => ({
    default: module.RegisterTutorPage,
  })),
);
const TutorOfferingPage = lazy(() =>
  import("@/features/identity/pages/TutorOfferingPage").then((module) => ({
    default: module.TutorOfferingPage,
  })),
);
const TutorDetailPage = lazy(() =>
  import("@/features/identity/pages/TutorDetailPage").then((module) => ({
    default: module.TutorDetailPage,
  })),
);
const TutorDirectoryPage = lazy(() =>
  import("@/features/identity/pages/TutorDirectoryPage").then((module) => ({
    default: module.TutorDirectoryPage,
  })),
);
const AdminPendingTutorsPage = lazy(() =>
  import("@/features/identity/pages/AdminPendingTutorsPage").then((module) => ({
    default: module.AdminPendingTutorsPage,
  })),
);
const RegisterStudentPage = lazy(() =>
  import("@/features/identity/pages/RegisterStudentPage").then((module) => ({
    default: module.RegisterStudentPage,
  })),
);
const StudentDetailPage = lazy(() =>
  import("@/features/identity/pages/StudentDetailPage").then((module) => ({
    default: module.StudentDetailPage,
  })),
);
const RegisterParentGuardianPage = lazy(() =>
  import("@/features/identity/pages/RegisterParentGuardianPage").then((module) => ({
    default: module.RegisterParentGuardianPage,
  })),
);
const ParentGuardianDetailPage = lazy(() =>
  import("@/features/identity/pages/ParentGuardianDetailPage").then((module) => ({
    default: module.ParentGuardianDetailPage,
  })),
);
const RelationshipsPage = lazy(() =>
  import("@/features/identity/pages/RelationshipsPage").then((module) => ({
    default: module.RelationshipsPage,
  })),
);

const DeclareAvailabilityPage = lazy(() =>
  import("@/features/scheduling/pages/DeclareAvailabilityPage").then((module) => ({
    default: module.DeclareAvailabilityPage,
  })),
);
const AvailabilitySlotDetailPage = lazy(() =>
  import("@/features/scheduling/pages/AvailabilitySlotDetailPage").then((module) => ({
    default: module.AvailabilitySlotDetailPage,
  })),
);
const BookSessionPage = lazy(() =>
  import("@/features/scheduling/pages/BookSessionPage").then((module) => ({
    default: module.BookSessionPage,
  })),
);
const SessionDetailPage = lazy(() =>
  import("@/features/scheduling/pages/SessionDetailPage").then((module) => ({
    default: module.SessionDetailPage,
  })),
);
const StudentSessionListPage = lazy(() =>
  import("@/features/scheduling/pages/StudentSessionListPage").then((module) => ({
    default: module.StudentSessionListPage,
  })),
);
const TutorSessionListPage = lazy(() =>
  import("@/features/scheduling/pages/TutorSessionListPage").then((module) => ({
    default: module.TutorSessionListPage,
  })),
);

const TutorSearchPage = lazy(() =>
  import("@/features/discovery/pages/TutorSearchPage").then((module) => ({
    default: module.TutorSearchPage,
  })),
);

const AdminDashboardPage = lazy(() =>
  import("@/features/oversight/pages/AdminDashboardPage").then((module) => ({
    default: module.AdminDashboardPage,
  })),
);
const GlobalSessionListPage = lazy(() =>
  import("@/features/oversight/pages/GlobalSessionListPage").then((module) => ({
    default: module.GlobalSessionListPage,
  })),
);

/**
 * The Phase D1 living reference page — dev-only. `import.meta.env.DEV` is
 * replaced with the literal `false` in a production build (Vite's `define`
 * transform runs before Rollup bundles), so this whole ternary, including
 * its `import()` call, is unreachable dead code there and is eliminated —
 * no StyleGuidePage chunk is emitted to `dist/` at all (verified by grep in
 * docs/phases/PHASE-D1-REPORT.md Task 5). It is never added below to
 * `router`'s `children` in production for the same reason: `devRoutes` is
 * `[]` there.
 */
const StyleGuidePage = import.meta.env.DEV
  ? lazy(() =>
      import("@/dev/StyleGuidePage").then((module) => ({ default: module.StyleGuidePage })),
    )
  : null;

/** Every routed page is code-split; this keeps the initial bundle limited to the app shell. */
function withSuspense(element: ReactNode) {
  return <Suspense fallback={<LoadingState label="Loading page…" />}>{element}</Suspense>;
}

/**
 * Phase 4.9 Task 4: a real route guard for routes whose backend permission
 * (RolePermissionCatalog) grants to specific roles only — reached directly
 * by URL, a role outside `roles` gets ForbiddenState instead of this
 * route's content, rather than just being hidden from the nav.
 */
function withRole(roles: ActorRole[], element: ReactNode) {
  return <RequireRole roles={roles}>{element}</RequireRole>;
}

const devRoutes =
  import.meta.env.DEV && StyleGuidePage
    ? [{ path: paths.dev.styleGuide, element: withSuspense(<StyleGuidePage />) }]
    : [];

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { path: paths.home, element: withSuspense(<DashboardPage />) },
      { path: paths.auth.login, element: withSuspense(<LoginPage />) },
      {
        path: paths.auth.resetPassword,
        element: withSuspense(withRole(["AdminStaff"], <AdminResetPasswordPage />)),
      },

      { path: paths.identity.tutorRegister, element: withSuspense(<RegisterTutorPage />) },
      { path: paths.identity.tutorDirectory, element: withSuspense(<TutorDirectoryPage />) },
      {
        path: paths.identity.tutorPending,
        element: withSuspense(withRole(["AdminStaff"], <AdminPendingTutorsPage />)),
      },
      {
        path: paths.identity.tutorEditPattern,
        element: withSuspense(withRole(["Tutor"], <TutorOfferingPage />)),
      },
      { path: paths.identity.tutorDetailPattern, element: withSuspense(<TutorDetailPage />) },

      { path: paths.identity.studentRegister, element: withSuspense(<RegisterStudentPage />) },
      {
        path: paths.identity.studentDetailBase,
        element: withSuspense(withRole(["Student", "ParentGuardian", "AdminStaff"], <StudentDetailPage />)),
      },
      {
        path: paths.identity.studentDetailPattern,
        element: withSuspense(withRole(["Student", "ParentGuardian", "AdminStaff"], <StudentDetailPage />)),
      },

      {
        path: paths.identity.parentGuardianRegister,
        element: withSuspense(<RegisterParentGuardianPage />),
      },
      {
        path: paths.identity.parentGuardianDetailBase,
        element: withSuspense(
          withRole(["Student", "ParentGuardian", "AdminStaff"], <ParentGuardianDetailPage />),
        ),
      },
      {
        path: paths.identity.parentGuardianDetailPattern,
        element: withSuspense(
          withRole(["Student", "ParentGuardian", "AdminStaff"], <ParentGuardianDetailPage />),
        ),
      },

      {
        path: paths.identity.relationships,
        element: withSuspense(withRole(["Student", "ParentGuardian", "AdminStaff"], <RelationshipsPage />)),
      },

      {
        path: paths.scheduling.declareAvailability,
        element: withSuspense(withRole(["Tutor"], <DeclareAvailabilityPage />)),
      },
      {
        path: paths.scheduling.availabilitySlotDetailBase,
        element: withSuspense(<AvailabilitySlotDetailPage />),
      },
      {
        path: paths.scheduling.availabilitySlotDetailPattern,
        element: withSuspense(<AvailabilitySlotDetailPage />),
      },
      {
        path: paths.scheduling.bookSession,
        element: withSuspense(withRole(["Student", "ParentGuardian"], <BookSessionPage />)),
      },
      {
        path: paths.scheduling.sessionDetailBase,
        element: withSuspense(<SessionDetailPage />),
      },
      {
        path: paths.scheduling.sessionDetailPattern,
        element: withSuspense(<SessionDetailPage />),
      },
      {
        path: paths.scheduling.studentScheduleBase,
        element: withSuspense(
          withRole(["Student", "ParentGuardian", "AdminStaff"], <StudentSessionListPage />),
        ),
      },
      {
        path: paths.scheduling.studentSchedulePattern,
        element: withSuspense(
          withRole(["Student", "ParentGuardian", "AdminStaff"], <StudentSessionListPage />),
        ),
      },
      {
        path: paths.scheduling.tutorScheduleBase,
        element: withSuspense(withRole(["Tutor", "AdminStaff"], <TutorSessionListPage />)),
      },
      {
        path: paths.scheduling.tutorSchedulePattern,
        element: withSuspense(withRole(["Tutor", "AdminStaff"], <TutorSessionListPage />)),
      },

      { path: paths.discovery.tutorSearch, element: withSuspense(<TutorSearchPage />) },

      {
        path: paths.oversight.adminDashboard,
        element: withSuspense(withRole(["AdminStaff"], <AdminDashboardPage />)),
      },
      {
        path: paths.oversight.globalSessions,
        element: withSuspense(withRole(["AdminStaff"], <GlobalSessionListPage />)),
      },

      ...devRoutes,

      { path: "*", element: withSuspense(<NotFoundPage />) },
    ],
  },
]);
