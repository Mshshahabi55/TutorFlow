import { useParams } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import {
  useSetTutorHourlyRate,
  useSetTutorLanguage,
  useSetTutorLocation,
  useSetTutorOfferedDurations,
  useSetTutorSubject,
} from "@/features/identity/hooks/useTutorMutations";
import {
  tutorOfferingSchema,
  type TutorOfferingFormValues,
} from "@/features/identity/validation/tutorOfferingSchema";
import {
  formatMinutesList,
  minutesToTimeSpan,
  parseMinutesList,
} from "@/shared/utils/duration";
import { toTomanInputValue, tomanToRial } from "@/shared/money/rial";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { PageHeader } from "@/shared/components/PageHeader";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { UnavailableState } from "@/shared/components/feedback/UnavailableState";
import { useNotification } from "@/shared/hooks/useNotification";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

function toFormValues(tutor: TutorDto): TutorOfferingFormValues {
  return {
    hourlyRate: tutor.hourlyRate !== null ? toTomanInputValue(tutor.hourlyRate) : "",
    subject: tutor.subject ?? "",
    language: tutor.language ?? "",
    location: tutor.location ?? "",
    offeredDurationsMinutes: formatMinutesList(tutor.offeredDurations),
  };
}

interface TutorOfferingFormProps {
  tutorId: string;
  tutor: TutorDto;
}

/**
 * One "Save" submits only the fields the Tutor actually changed (react-hook-
 * form's dirtyFields), each as its own PATCH — the backend has no combined
 * "update offering" capability, only five independent ones.
 */
function TutorOfferingForm({ tutorId, tutor }: TutorOfferingFormProps) {
  const { notify } = useNotification();
  const setHourlyRate = useSetTutorHourlyRate(tutorId);
  const setSubject = useSetTutorSubject(tutorId);
  const setLanguage = useSetTutorLanguage(tutorId);
  const setLocation = useSetTutorLocation(tutorId);
  const setOfferedDurations = useSetTutorOfferedDurations(tutorId);

  const form = useForm<TutorOfferingFormValues>({
    resolver: zodResolver(tutorOfferingSchema),
    defaultValues: toFormValues(tutor),
  });

  const mutations = [setHourlyRate, setSubject, setLanguage, setLocation, setOfferedDurations];
  const isSaving = mutations.some((mutation) => mutation.isPending);
  const failedMutation = mutations.find((mutation) => mutation.isError);

  // react-hook-form only keeps dirtyFields live if it's read during render
  // (its formState is a lazily-subscribed Proxy) — reading it only inside
  // the async submit handler below would silently see an empty snapshot.
  const { dirtyFields } = form.formState;

  async function handleSubmit(values: TutorOfferingFormValues) {
    const updates: Promise<void>[] = [];

    if (dirtyFields.hourlyRate) {
      updates.push(setHourlyRate.mutateAsync(tomanToRial(Number(values.hourlyRate))));
    }
    if (dirtyFields.subject) {
      updates.push(setSubject.mutateAsync(values.subject));
    }
    if (dirtyFields.language) {
      updates.push(setLanguage.mutateAsync(values.language));
    }
    if (dirtyFields.location) {
      updates.push(setLocation.mutateAsync(values.location));
    }
    if (dirtyFields.offeredDurationsMinutes) {
      const durations = parseMinutesList(values.offeredDurationsMinutes).map(minutesToTimeSpan);
      updates.push(setOfferedDurations.mutateAsync(durations));
    }

    if (updates.length === 0) {
      notify({ message: "Nothing to save — no field changed.", severity: "info" });
      return;
    }

    try {
      await Promise.all(updates);
      notify({ message: "Offering updated.", severity: "success" });
      form.reset(values);
    } catch {
      notify({ message: "Some changes could not be saved.", severity: "error" });
    }
  }

  return (
    <Form form={form} onSubmit={handleSubmit}>
      <Stack spacing={2}>
        <FormTextField name="hourlyRate" label="Hourly rate (Toman)" inputMode="numeric" />
        <FormTextField name="subject" label="Subject" />
        <FormTextField name="language" label="Language" />
        <FormTextField name="location" label="Location" />
        <FormTextField
          name="offeredDurationsMinutes"
          label="Offered durations (minutes, comma-separated)"
          placeholder="30, 60, 90"
        />
        {failedMutation ? (
          <ErrorState error={failedMutation.error} title="Some changes could not be saved" />
        ) : null}
        <Button
          type="submit"
          variant="contained"
          disabled={isSaving}
          sx={{ alignSelf: "flex-start" }}
        >
          {isSaving ? "Saving…" : "Save changes"}
        </Button>
      </Stack>
    </Form>
  );
}

export function TutorOfferingPage() {
  const { tutorId } = useParams<{ tutorId: string }>();
  const tutorQuery = useTutor(tutorId);

  return (
    <Stack spacing={3} maxWidth={560}>
      <PageHeader
        title="Manage Tutor offering"
        subtitle={
          <Typography variant="body2" color="text.secondary" fontFamily="ui-monospace, monospace">
            {tutorId}
          </Typography>
        }
      />

      <Card variant="outlined">
        <CardContent>
          {tutorQuery.isPending ? <LoadingState label="Loading Tutor…" /> : null}
          {tutorQuery.isError ? (
            <UnavailableState
              title="Tutor unavailable"
              description="This listing could not be loaded. It may have been removed, or the link might be broken."
              actions={[
                { label: "Try again", onClick: () => void tutorQuery.refetch() },
                { label: "Back to Dashboard", to: paths.home, variant: "contained" },
              ]}
              headingComponent="h2"
            />
          ) : null}
          {tutorQuery.isSuccess ? (
            <TutorOfferingForm tutorId={tutorId as string} tutor={tutorQuery.data} />
          ) : null}
        </CardContent>
      </Card>
    </Stack>
  );
}
