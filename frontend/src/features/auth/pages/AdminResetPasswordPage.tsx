import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAdminResetPassword } from "@/features/auth/hooks/useAdminResetPassword";
import {
  resetPasswordSchema,
  type ResetPasswordFormValues,
} from "@/features/auth/validation/resetPasswordSchema";
import { PageHeader } from "@/shared/components/PageHeader";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useNotification } from "@/shared/hooks/useNotification";

/**
 * Admin-assisted password reset only — no self-service recovery flow exists
 * yet (docs/adr/ADR-017-authentication-mechanism-decision.md). Resetting
 * immediately revokes every active session for the account. Not yet
 * restricted to Admin/Staff at the API level — role-based endpoint
 * protection is a separate, not-yet-built capability (Launch Preparation,
 * Priority 2), the same honest limitation already disclosed for the rest of
 * the app.
 */
export function AdminResetPasswordPage() {
  const resetPassword = useAdminResetPassword();
  const { notify } = useNotification();

  const form = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { accountId: "", newPassword: "" },
  });

  function handleSubmit(values: ResetPasswordFormValues) {
    resetPassword.mutate(
      { accountId: values.accountId, newPassword: values.newPassword },
      {
        onSuccess: () => {
          notify({
            message: "Password reset. Every existing session for this account was signed out.",
            severity: "success",
          });
          form.reset();
        },
      },
    );
  }

  return (
    <Stack spacing={3} maxWidth={480}>
      <PageHeader
        title="Reset account password"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Resetting a password immediately signs that account out of every device.
          </Typography>
        }
      />

      <Card variant="outlined">
        <CardContent>
          <Form form={form} onSubmit={handleSubmit}>
            <Stack spacing={2} alignItems="flex-start">
              <FormTextField name="accountId" label="Account id" fullWidth />
              <FormTextField
                name="newPassword"
                label="New password"
                type="password"
                autoComplete="new-password"
                fullWidth
              />
              {resetPassword.isError ? (
                <ErrorState error={resetPassword.error} title="Could not reset password" />
              ) : null}
              <Button type="submit" variant="contained" disabled={resetPassword.isPending}>
                {resetPassword.isPending ? "Resetting…" : "Reset password"}
              </Button>
            </Stack>
          </Form>
        </CardContent>
      </Card>
    </Stack>
  );
}
