import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRegisterStudent } from "@/features/identity/hooks/useStudentMutations";
import {
  studentRegistrationSchema,
  type StudentRegistrationFormValues,
} from "@/features/identity/validation/studentRegistrationSchema";
import { CopyableId } from "@/shared/components/CopyableId";
import { PageHeader } from "@/shared/components/PageHeader";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { FormCheckbox } from "@/shared/components/forms/FormCheckbox";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useNotification } from "@/shared/hooks/useNotification";

/**
 * POST /students accepts email, password, and IsMinor — IsMinor supplied as
 * an already-known fact (RegisterStudentCommand.cs); no age/birthdate input
 * exists because the backend does not compute minor status itself.
 */
export function RegisterStudentPage() {
  const registerStudent = useRegisterStudent();
  const { notify } = useNotification();

  const form = useForm<StudentRegistrationFormValues>({
    resolver: zodResolver(studentRegistrationSchema),
    defaultValues: { email: "", password: "", isMinor: false },
  });

  function handleSubmit(values: StudentRegistrationFormValues) {
    registerStudent.mutate(values, {
      onSuccess: () => notify({ message: "Student account registered.", severity: "success" }),
    });
  }

  return (
    <Stack spacing={3} maxWidth={560}>
      <PageHeader
        title="Register as a Student"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            A minor Student requires a confirmed Parent/Guardian Relationship before booking —
            that relationship is set up separately, after registration.
          </Typography>
        }
      />

      <Card variant="outlined">
        <CardContent>
          {registerStudent.isSuccess ? (
            <Stack spacing={2}>
              <Typography variant="subtitle1" fontWeight={600}>
                Your Student account was created
              </Typography>
              <CopyableId id={registerStudent.data.studentId} />
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
                <FormCheckbox name="isMinor" label="This Student is a minor" />
                {registerStudent.isError ? (
                  <ErrorState error={registerStudent.error} title="Registration failed" />
                ) : null}
                <Button type="submit" variant="contained" disabled={registerStudent.isPending}>
                  {registerStudent.isPending ? "Registering…" : "Register as Student"}
                </Button>
              </Stack>
            </Form>
          )}
        </CardContent>
      </Card>
    </Stack>
  );
}
