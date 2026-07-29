import { useState } from "react";
import { Box, Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { PageHeader } from "@/shared/components/PageHeader";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  useConfirmRelationship,
  useInviteRelationship,
} from "@/features/identity/hooks/useRelationshipMutations";
import { useRelationshipsForAccount } from "@/features/identity/hooks/useRelationshipQueries";
import {
  relationshipInviteSchema,
  type RelationshipInviteFormValues,
} from "@/features/identity/validation/relationshipInviteSchema";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { CopyableId } from "@/shared/components/CopyableId";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { DataTable, type DataTableColumn } from "@/shared/components/table/DataTable";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { ChildSummaryCard } from "@/features/identity/components/ChildSummaryCard";
import { ChildSummaryCardSkeleton } from "@/features/identity/components/ChildSummaryCardSkeleton";
import { IdentityGate } from "@/shared/components/IdentityGate";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { IdentityLookupErrorState } from "@/shared/components/feedback/IdentityLookupErrorState";
import { useNotification } from "@/shared/hooks/useNotification";
import { useOwnId } from "@/shared/hooks/useOwnId";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import { RelationshipStatus } from "@/services/api/dtos";
import type { RelationshipDto } from "@/services/api/dtos";

/**
 * Reached by a Student or Admin/Staff viewer (`RelationshipsPage` sends a
 * Parent/Guardian to `ParentChildrenView`/`AddChildCard` instead). A real
 * signed-in Student's own id is never re-entered (`useOwnId`) — only the
 * Parent/Guardian's id remains manual, since no directory exists to look
 * one up by anything else. An Admin/Staff viewer (or the dev-only "Acting
 * as" preview) sees both fields, the same generic tool as before.
 */
function InviteRelationshipCard() {
  const { notify } = useNotification();
  const { id: ownStudentId, isFromSession } = useOwnId("student");
  const inviteRelationship = useInviteRelationship();
  const form = useForm<RelationshipInviteFormValues>({
    resolver: zodResolver(relationshipInviteSchema),
    defaultValues: { parentGuardianId: "", studentId: ownStudentId ?? "" },
  });

  function handleSubmit(values: RelationshipInviteFormValues) {
    inviteRelationship.mutate(values, {
      onSuccess: () => {
        notify({ message: "Relationship invitation sent.", severity: "success" });
        form.reset({ parentGuardianId: "", studentId: ownStudentId ?? "" });
      },
    });
  }

  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="subtitle1" fontWeight={600} gutterBottom>
          Invite a Relationship
        </Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          A Relationship becomes active only once the invited party confirms it.
        </Typography>

        {inviteRelationship.isSuccess ? (
          <Stack spacing={1} mt={1}>
            <Typography variant="body2">Invitation created:</Typography>
            <CopyableId id={inviteRelationship.data.relationshipId} />
          </Stack>
        ) : (
          <Form form={form} onSubmit={handleSubmit}>
            <Stack spacing={2} mt={1} alignItems="flex-start">
              <FormTextField name="parentGuardianId" label="Parent/Guardian id" fullWidth />
              {isFromSession ? null : (
                <FormTextField name="studentId" label="Student id" fullWidth />
              )}
              <Button type="submit" variant="contained" disabled={inviteRelationship.isPending}>
                {inviteRelationship.isPending ? "Inviting…" : "Send invitation"}
              </Button>
            </Stack>
          </Form>
        )}
      </CardContent>
    </Card>
  );
}

function ConfirmRelationshipAction({ relationship }: { relationship: RelationshipDto }) {
  const confirmRelationship = useConfirmRelationship();
  const { notify } = useNotification();

  if (relationship.status !== RelationshipStatus.Invited) {
    return null;
  }

  return (
    <Button
      size="small"
      variant="contained"
      disabled={confirmRelationship.isPending}
      onClick={() =>
        confirmRelationship.mutate(relationship.relationshipId, {
          onSuccess: () => notify({ message: "Relationship confirmed.", severity: "success" }),
        })
      }
    >
      Confirm
    </Button>
  );
}

const columns: DataTableColumn<RelationshipDto>[] = [
  { key: "relationshipId", header: "Relationship id", render: (row) => row.relationshipId },
  { key: "parentGuardianId", header: "Parent/Guardian id", render: (row) => row.parentGuardianId },
  { key: "studentId", header: "Student id", render: (row) => row.studentId },
  {
    key: "status",
    header: "Status",
    render: (row) => (
      <StatusPill
        label={row.status === RelationshipStatus.Confirmed ? "Confirmed" : "Invited"}
        tone={row.status === RelationshipStatus.Confirmed ? "success" : "warning"}
      />
    ),
  },
  {
    key: "actions",
    header: "",
    align: "right",
    render: (row) => <ConfirmRelationshipAction relationship={row} />,
  },
];

function RelationshipsForAccountCard() {
  const [accountId, setAccountId] = useState<string | undefined>(undefined);
  const relationshipsQuery = useRelationshipsForAccount(accountId);

  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="subtitle1" fontWeight={600} gutterBottom>
          View Relationships for an account
        </Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Works for either a Parent/Guardian id or a Student id — both are Accounts.
        </Typography>

        <Box mt={1} mb={2}>
          <IdLookupForm label="Account id" onSubmit={setAccountId} />
        </Box>

        {accountId ? (
          <DataTable
            columns={columns}
            rows={relationshipsQuery.data ?? []}
            getRowKey={(row) => row.relationshipId}
            isLoading={relationshipsQuery.isPending}
            error={relationshipsQuery.isError ? relationshipsQuery.error : undefined}
            onRetry={() => void relationshipsQuery.refetch()}
            emptyState={{
              title: "No Relationships for this account",
              description: "Invite one above to get started.",
            }}
          />
        ) : null}
      </CardContent>
    </Card>
  );
}

/**
 * A friendlier, card-based add-a-child form for the Parent's own "My
 * Children" view — same `useInviteRelationship` mutation and
 * `relationshipInviteSchema` as `InviteRelationshipCard`, but the Parent's
 * own id resolves automatically from the real session (`useOwnId`) rather
 * than asked for again, so only the Student's id needs entering.
 */
function AddChildCard() {
  const { notify } = useNotification();
  const { id: ownParentGuardianId } = useOwnId("parentGuardian");
  const inviteRelationship = useInviteRelationship();
  const form = useForm<RelationshipInviteFormValues>({
    resolver: zodResolver(relationshipInviteSchema),
    defaultValues: { parentGuardianId: ownParentGuardianId ?? "", studentId: "" },
  });

  function handleSubmit(values: RelationshipInviteFormValues) {
    inviteRelationship.mutate(values, {
      onSuccess: () => {
        notify({ message: "Invitation sent — it will show up here once confirmed.", severity: "success" });
        form.reset({ parentGuardianId: values.parentGuardianId, studentId: "" });
      },
    });
  }

  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="subtitle1" fontWeight={600} gutterBottom>
          Add a Child
        </Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Enter your child&rsquo;s Student id — they&rsquo;ll show up here once they confirm.
        </Typography>

        <Form form={form} onSubmit={handleSubmit}>
          <Stack spacing={2} mt={1} alignItems={{ xs: "stretch", sm: "flex-start" }}>
            <FormTextField name="studentId" label="Student id" fullWidth />
            <Button type="submit" variant="contained" disabled={inviteRelationship.isPending}>
              {inviteRelationship.isPending ? "Sending…" : "Send invitation"}
            </Button>
          </Stack>
        </Form>
      </CardContent>
    </Card>
  );
}

/** Card-based "My Children" — reuses the same `useRelationshipsForAccount` query the generic Relationships view uses, presented as ChildSummaryCards instead of a raw table row per child. */
function MyChildrenView({ accountId, onChooseAgain }: { accountId: string; onChooseAgain: () => void }) {
  const relationshipsQuery = useRelationshipsForAccount(accountId);

  if (relationshipsQuery.isPending) {
    return (
      <Stack spacing={2}>
        <ChildSummaryCardSkeleton />
        <ChildSummaryCardSkeleton />
      </Stack>
    );
  }

  if (relationshipsQuery.isError) {
    return (
      <IdentityLookupErrorState
        error={relationshipsQuery.error}
        onRetry={() => void relationshipsQuery.refetch()}
        onChooseAgain={onChooseAgain}
      />
    );
  }

  const relationships = relationshipsQuery.data;

  if (relationships.length === 0) {
    return (
      <EmptyState
        title="You haven't added a child yet"
        description="Add your child's Student id below to get started."
      />
    );
  }

  return (
    <Stack spacing={2}>
      {relationships.map((relationship) => (
        <ChildSummaryCard key={relationship.relationshipId} relationship={relationship} />
      ))}
    </Stack>
  );
}

/**
 * A Parent/Guardian sees a warm, card-based "My Children" view of this
 * same page (reusing `IdentityGate` so their own id is asked for once,
 * never re-typed). A Student or AdminStaff viewer — this route is shared
 * with both (`router.tsx`) since either side of a Relationship, or an
 * Admin, may need to look one up — still sees the original generic
 * lookup-by-account-id/DataTable experience: "My Children" would misframe
 * a Student looking at their own Parent/Guardian relationships, and an
 * Admin genuinely needs the raw, any-account lookup tool.
 */
function ParentChildrenView() {
  return (
    <Stack spacing={3} maxWidth={720}>
      <PageHeader
        title="My Children"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            See your children and add a new one — a Relationship becomes active once they confirm
            it.
          </Typography>
        }
      />

      <IdentityGate
        kind="parentGuardian"
        fieldLabel="Parent/Guardian id"
        title="Let's find your children"
        description="Enter your Parent/Guardian id once — we'll remember it on this device so you won't need to again."
      >
        {(accountId, forget) => <MyChildrenView accountId={accountId} onChooseAgain={forget} />}
      </IdentityGate>

      <AddChildCard />
    </Stack>
  );
}

export function RelationshipsPage() {
  const role = useEffectiveRole();

  if (role === "ParentGuardian") {
    return <ParentChildrenView />;
  }

  return (
    <Stack spacing={3} maxWidth={720}>
      <PageHeader
        title="Relationships"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            A Parent/Guardian–Student Relationship is established by invitation and confirmed by
            the other party — never created unilaterally.
          </Typography>
        }
      />

      <InviteRelationshipCard />
      <RelationshipsForAccountCard />
    </Stack>
  );
}
