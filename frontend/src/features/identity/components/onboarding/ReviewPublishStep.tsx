import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import PublishRoundedIcon from "@mui/icons-material/PublishRounded";
import WarningAmberRoundedIcon from "@mui/icons-material/WarningAmberRounded";
import { SectionCard } from "@/shared/components/SectionCard";
import { ProfileCompletionCard } from "@/features/identity/components/ProfileCompletionCard";
import { deriveProfileCompletion } from "@/features/identity/utils/profileCompletion";
import { formatMinutesList } from "@/shared/utils/duration";
import { formatToman } from "@/shared/money/rial";
import type { TutorDto } from "@/services/api/dtos";

function PreviewRow({ label, value, warn }: { label: string; value: string; warn?: boolean }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      {warn ? (
        <Stack direction="row" spacing={0.5} alignItems="center">
          <WarningAmberRoundedIcon color="warning" fontSize="small" aria-hidden="true" />
          <Typography variant="body1" fontWeight={600} color="warning.main">
            {value}
          </Typography>
        </Stack>
      ) : (
        <Typography variant="body1" fontWeight={600}>
          {value}
        </Typography>
      )}
    </Box>
  );
}

function EditSectionButton({ stepIndex, onEditSection }: { stepIndex: number; onEditSection?: (stepIndex: number) => void }) {
  if (!onEditSection) {
    return null;
  }
  return (
    <Button size="small" onClick={() => onEditSection(stepIndex)}>
      Edit
    </Button>
  );
}

export interface ReviewPublishStepProps {
  tutor: TutorDto;
  hasAvailability: boolean;
  onPublish: () => void;
  isPublishing: boolean;
  publishError?: string;
  /** Jumps the wizard back to the given step index — omitted in isolated unit tests, where there's no wizard shell to jump within. */
  onEditSection?: (stepIndex: number) => void;
}

/**
 * Step 7 — Review & Publish. Reads from the already-persisted `tutor`
 * (every prior step autosaved via its own PATCH on "Next") rather than
 * duplicating in-progress form state — the server is the source of truth
 * by the time a Tutor reaches this step. "Publish" calls
 * `SubmitTutorProfileCommand`, which the backend itself rejects unless a
 * Subject and an Hourly Rate are set (ADR-024) — the readiness banner and
 * the warning highlights below surface that same requirement before the
 * Tutor even clicks it.
 */
export function ReviewPublishStep({ tutor, hasAvailability, onPublish, isPublishing, publishError, onEditSection }: ReviewPublishStepProps) {
  const completion = deriveProfileCompletion(tutor, hasAvailability);
  const readyToPublish = tutor.subject !== null && tutor.hourlyRate !== null;
  const canPublish = readyToPublish && tutor.profileStatus !== "Submitted";

  return (
    <Stack spacing={3}>
      {tutor.profileStatus !== "Submitted" ? (
        <Alert severity={readyToPublish ? "success" : "warning"}>
          {readyToPublish
            ? "You're ready to publish."
            : "A primary subject and an hourly rate are required before you can publish."}
        </Alert>
      ) : null}

      <SectionCard title="Personal information" action={<EditSectionButton stepIndex={0} onEditSection={onEditSection} />}>
        <Stack spacing={2}>
          <PreviewRow label="Display name" value={tutor.displayName ?? tutor.subject ?? "Not set"} />
          <PreviewRow label="Headline" value={tutor.headline ?? "Not set"} />
          <PreviewRow label="Native language" value={tutor.language ?? "Not set"} />
        </Stack>
      </SectionCard>

      <SectionCard title="Teaching & subjects" action={<EditSectionButton stepIndex={1} onEditSection={onEditSection} />}>
        <Stack spacing={2}>
          <PreviewRow label="Primary subject" value={tutor.subject ?? "Required"} warn={tutor.subject === null} />
          <PreviewRow
            label="Additional subjects"
            value={
              tutor.tutorSubjects && tutor.tutorSubjects.length > 0
                ? tutor.tutorSubjects.map((entry) => (entry.level ? `${entry.subject} (${entry.level})` : entry.subject)).join(", ")
                : "None"
            }
          />
        </Stack>
      </SectionCard>

      <SectionCard title="Pricing" action={<EditSectionButton stepIndex={3} onEditSection={onEditSection} />}>
        <Stack spacing={2}>
          <PreviewRow
            label="Hourly rate"
            value={tutor.hourlyRate !== null ? `${formatToman(tutor.hourlyRate)} Toman/hr` : "Required"}
            warn={tutor.hourlyRate === null}
          />
          <PreviewRow
            label="Session lengths"
            value={tutor.offeredDurations.length > 0 ? `${formatMinutesList(tutor.offeredDurations)} minutes` : "Not set"}
          />
          <PreviewRow
            label="Trial lesson"
            value={
              tutor.trialLessonAvailable
                ? `Available${tutor.trialLessonPrice != null ? ` — ${formatToman(tutor.trialLessonPrice)} Toman` : ""}`
                : "Not offered"
            }
          />
        </Stack>
      </SectionCard>

      <Box>
        <Typography variant="h6" component="h3" gutterBottom>
          Completion checklist
        </Typography>
        <ProfileCompletionCard completion={completion} tutorId={tutor.tutorId} />
      </Box>

      {publishError ? <Alert severity="error">{publishError}</Alert> : null}

      {tutor.profileStatus === "Submitted" ? (
        <Alert severity="success">Your profile has been submitted and is awaiting Admin review.</Alert>
      ) : (
        <Button
          type="button"
          variant="contained"
          size="large"
          startIcon={<PublishRoundedIcon />}
          onClick={onPublish}
          disabled={!canPublish || isPublishing}
          sx={{ alignSelf: "flex-start" }}
        >
          {isPublishing ? "Publishing…" : "Publish profile"}
        </Button>
      )}
    </Stack>
  );
}
