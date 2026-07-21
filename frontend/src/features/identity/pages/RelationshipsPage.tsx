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
import { useNotification } from "@/shared/hooks/useNotification";
import { RelationshipStatus } from "@/services/api/dtos";
import type { RelationshipDto } from "@/services/api/dtos";

function InviteRelationshipCard() {
  const { notify } = useNotification();
  const inviteRelationship = useInviteRelationship();
  const form = useForm<RelationshipInviteFormValues>({
    resolver: zodResolver(relationshipInviteSchema),
    defaultValues: { parentGuardianId: "", studentId: "" },
  });

  function handleSubmit(values: RelationshipInviteFormValues) {
    inviteRelationship.mutate(values, {
      onSuccess: () => {
        notify({ message: "Relationship invitation sent.", severity: "success" });
        form.reset();
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
              <FormTextField name="studentId" label="Student id" fullWidth />
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

export function RelationshipsPage() {
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
