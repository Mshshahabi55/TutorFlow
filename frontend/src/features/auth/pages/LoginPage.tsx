import { useNavigate } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useLogin } from "@/features/auth/hooks/useLogin";
import { loginSchema, type LoginFormValues } from "@/features/auth/validation/loginSchema";
import { PageHeader } from "@/shared/components/PageHeader";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useNotification } from "@/shared/hooks/useNotification";
import { paths } from "@/routes/paths";

export function LoginPage() {
  const navigate = useNavigate();
  const login = useLogin();
  const { notify } = useNotification();

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  function handleSubmit(values: LoginFormValues) {
    login.mutate(values, {
      onSuccess: () => {
        notify({ message: "Signed in.", severity: "success" });
        void navigate(paths.home);
      },
    });
  }

  return (
    <Stack spacing={3} maxWidth={420} mx="auto">
      <PageHeader
        title="Sign in"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Enter your email and password to sign in to TutorFlow.
          </Typography>
        }
      />

      <Card variant="outlined">
        <CardContent>
          <Form form={form} onSubmit={handleSubmit}>
            <Stack spacing={2} alignItems="flex-start">
              <FormTextField name="email" label="Email" type="email" autoComplete="email" fullWidth />
              <FormTextField
                name="password"
                label="Password"
                type="password"
                autoComplete="current-password"
                fullWidth
              />
              {login.isError ? (
                <ErrorState error={login.error} title="Could not sign in" />
              ) : null}
              <Button type="submit" variant="contained" disabled={login.isPending} fullWidth>
                {login.isPending ? "Signing in…" : "Sign in"}
              </Button>
            </Stack>
          </Form>
        </CardContent>
      </Card>
    </Stack>
  );
}
