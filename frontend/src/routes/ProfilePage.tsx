import { Navigate } from "react-router-dom";
import { Stack } from "@mui/material";
import { PageHeader } from "@/shared/components/PageHeader";
import { IdentityGate } from "@/shared/components/IdentityGate";
import { useRememberedId } from "@/shared/hooks/useRememberedId";
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
 * workspace needing its own id-aware profile link. Resolves the same
 * remembered id (`useRememberedId`) the rest of the app now shares — if
 * it's already known, this redirects straight to the existing detail
 * page (`TutorDetailPage`/`StudentDetailPage`/`ParentGuardianDetailPage`,
 * none of which changed); if not, `IdentityGate` asks once and remembers
 * it for every other screen too.
 */
export function ProfilePage() {
  const role = useEffectiveRole();
  const kind: IdentityKind =
    role === "Tutor" ? "tutor" : role === "ParentGuardian" ? "parentGuardian" : "student";
  const { id } = useRememberedId(kind);

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
