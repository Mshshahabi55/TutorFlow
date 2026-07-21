import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRegisterParentGuardian } from "@/features/identity/hooks/useParentGuardianMutations";
import {
  parentGuardianRegistrationSchema,
  type ParentGuardianRegistrationFormValues,
} from "@/features/identity/validation/parentGuardianRegistrationSchema";
import { CopyableId } from "@/shared/components/CopyableId";
import { PageHeader } from "@/shared/components/PageHeader";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useNotification } from "@/shared/hooks/useNotification";

/** POST /parent-guardians accepts email and password (docs/adr/ADR-017-authentication-mechanism-decision.md). */
export function RegisterParentGuardianPage() {
  const registerParentGuardian = useRegisterParentGuardian();
  const { notify } = useNotification();

  const form = useForm<ParentGuardianRegistrationFormValues>({
    resolver: zodResolver(parentGuardianRegistrationSchema),
    defaultValues: { email: "", password: "" },
  });

  function handleSubmit(values: ParentGuardianRegistrationFormValues) {
    registerParentGuardian.mutate(values, {
      onSuccess: () => notify({ message: "Parent/Guardian account registered.", severity: "success" }),
    });
  }

  return (
    <Stack spacing={3} maxWidth={560}>
      <PageHeader
        title="Register as a Parent/Guardian"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Link to a Student afterward by inviting a Relationship.
          </Typography>
        }
      />

      <Card variant="outlined">
        <CardContent>
          {registerParentGuardian.isSuccess ? (
            <Stack spacing={2}>
              <Typography variant="subtitle1" fontWeight={600}>
                Your Parent/Guardian account was created
              </Typography>
              <CopyableId id={registerParentGuardian.data.parentGuardianId} />
            </Stack>
          ) : (
            <Form form={form} onSubmit={handleSubmit}>
              <Stack spacing={2} alignItems="flex-start">
                <FormTextField name="email" label="Email" type="email" autoComplete="email" fullWidth />
                <FormTextField
                  name="password"
                  label="Password"
                  type="password"
                  autoComplete="new-password"
                  fullWidth
                />
                {registerParentGuardian.isError ? (
                  <ErrorState error={registerParentGuardian.error} title="Registration failed" />
                ) : null}
                <Button
                  type="submit"
                  variant="contained"
                  disabled={registerParentGuardian.isPending}
                >
                  {registerParentGuardian.isPending ? "Registering…" : "Register as Parent/Guardian"}
                </Button>
              </Stack>
            </Form>
          )}
        </CardContent>
      </Card>
    </Stack>
  );
}
