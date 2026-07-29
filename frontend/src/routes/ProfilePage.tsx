import { Navigate } from "react-router-dom";
import { Stack } from "@mui/material";
import { PageHeader } from "@/shared/components/PageHeader";
import { IdentityGate } from "@/shared/components/IdentityGate";
import { useOwnId } from "@/shared/hooks/useOwnId";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import { paths } from "@/routes/paths";

type IdentityKind = "tutor" | "student" | "parentGuardian";

function detailPathFor(kind: IdentityKind, id: string): string {
  if (kind === "tutor") {
    return paths.identity.tutorDetail(id);
  }
  if (kind === "parentGuardian") {
    return paths.identity.parentGuardianDetail(id);
  }
  return paths.identity.studentDetail(id);
}

const FIELD_LABEL: Record<IdentityKind, string> = {
  tutor: "Tutor id",
  student: "Student id",
  parentGuardian: "Parent/Guardian id",
};

/**
 * RC2: one "Profile" nav destination for every role, instead of each
 * workspace needing its own id-aware profile link. RC4.3: a real Tutor/
 * Student/Parent-Guardian's own id resolves automatically (`useOwnId`) —
 * this redirects straight to the existing detail page
 * (`TutorDetailPage`/`StudentDetailPage`/`ParentGuardianDetailPage`, none of
 * which changed) without ever asking. Admin/Staff has no detail page of its
 * own (no such profile view exists in this API) — sent to the Admin
 * dashboard instead of silently mismapping onto the Student detail page, a
 * pre-existing gap this phase also closes.
 */
export function ProfilePage() {
  const role = useEffectiveRole();
  // A stable, always-called hook regardless of role — Admin/Staff's `kind`
  // value is never actually used (handled by its own early return below),
  // but the hook itself must run unconditionally (Rules of Hooks).
  const kind: IdentityKind =
    role === "Tutor" ? "tutor" : role === "ParentGuardian" ? "parentGuardian" : "student";
  const { id } = useOwnId(kind);

  if (role === "AdminStaff") {
    return <Navigate to={paths.oversight.adminDashboard} replace />;
  }

  if (id) {
    return <Navigate to={detailPathFor(kind, id)} replace />;
  }

  return (
    <Stack spacing={3}>
      <PageHeader title="Profile" />
      <IdentityGate
        kind={kind}
        fieldLabel={FIELD_LABEL[kind]}
        title="Let's find your profile"
        description="Enter your id once — we'll remember it on this device so you won't need to again."
      >
        {(enteredId) => <Navigate to={detailPathFor(kind, enteredId)} replace />}
      </IdentityGate>
    </Stack>
  );
}
