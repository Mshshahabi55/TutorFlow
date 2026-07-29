import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRegisterTutor } from "@/features/identity/hooks/useTutorMutations";
import {
  tutorRegistrationSchema,
  type TutorRegistrationFormValues,
} from "@/features/identity/validation/tutorRegistrationSchema";
import { CopyableId } from "@/shared/components/CopyableId";
import { PageHeader } from "@/shared/components/PageHeader";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useNotification } from "@/shared/hooks/useNotification";
import { paths } from "@/routes/paths";

/**
 * POST /tutors takes an email and password (docs/adr/ADR-017-authentication-mechanism-decision.md)
 * — the only output otherwise is a new Tutor id. An Admin must approve the
 * account before it's discoverable, and rate/subject/language/location/
 * durations are set afterward.
 */
export function RegisterTutorPage() {
  const registerTutor = useRegisterTutor();
  const { notify } = useNotification();

  const form = useForm<TutorRegistrationFormValues>({
    resolver: zodResolver(tutorRegistrationSchema),
    defaultValues: { email: "", password: "" },
  });

  function handleSubmit(values: TutorRegistrationFormValues) {
    registerTutor.mutate(values, {
      onSuccess: () => notify({ message: "Tutor account registered.", severity: "success" }),
    });
  }

  return (
    <Stack spacing={3} maxWidth={560}>
      <PageHeader
        title="Register as a Tutor"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            An Admin must approve your account before you're discoverable, and you'll set your
            rate, subject, language, location, and offered durations afterward.
          </Typography>
        }
      />

      <Card variant="outlined">
        <CardContent>
          {registerTutor.isSuccess ? (
            <Stack spacing={2}>
              <Typography variant="subtitle1" fontWeight={600}>
                Your Tutor account was created
              </Typography>
              <CopyableId id={registerTutor.data.tutorId} />
              <Button
                component={RouterLink}
                to={paths.identity.tutorOnboarding(registerTutor.data.tutorId)}
                variant="contained"
                sx={{ alignSelf: "flex-start" }}
              >
                Complete your profile
              </Button>
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
                {registerTutor.isError ? (
                  <ErrorState error={registerTutor.error} title="Registration failed" />
                ) : null}
                <Button type="submit" variant="contained" disabled={registerTutor.isPending}>
                  {registerTutor.isPending ? "Registering…" : "Register as Tutor"}
                </Button>
              </Stack>
            </Form>
          )}
        </CardContent>
      </Card>
    </Stack>
  );
}
