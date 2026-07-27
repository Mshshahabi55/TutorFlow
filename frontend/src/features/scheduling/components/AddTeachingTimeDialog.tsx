import { Button, Dialog, DialogContent, DialogTitle, IconButton, Stack } from "@mui/material";
import CloseRoundedIcon from "@mui/icons-material/CloseRounded";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
import { useDeclareAvailability } from "@/features/scheduling/hooks/useAvailabilitySlotMutations";
import {
  declareAvailabilitySchema,
  type DeclareAvailabilityFormValues,
} from "@/features/scheduling/validation/declareAvailabilitySchema";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { FormSelect } from "@/shared/components/forms/FormSelect";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useNotification } from "@/shared/hooks/useNotification";
import { fromTehranInput } from "@/shared/time/tehranTime";
import { minutesToTimeSpan } from "@/shared/utils/duration";

const DELIVERY_MODE_OPTIONS = [
  { value: "0", label: "Online" },
  { value: "1", label: "In-Person" },
];

export interface AddTeachingTimeDialogProps {
  tutorId: string;
  open: boolean;
  onClose: () => void;
  /** Prefills the start-time field, e.g. when a Tutor clicks an empty day on the calendar. */
  initialDateKey?: string;
}

/**
 * RC2.2: "never expose raw forms first — calendar first, click, select,
 * save, minimal typing." The Tutor id is already known from the calling
 * page's own `IdentityGate` — this dialog never asks for it again, unlike
 * the previous always-visible form. Same `POST /availability-slots`
 * mutation as before (`useDeclareAvailability`), which now also
 * invalidates the calendar's own query on success, so the new slot
 * appears immediately without closing/reopening the page.
 */
export function AddTeachingTimeDialog({
  tutorId,
  open,
  onClose,
  initialDateKey,
}: AddTeachingTimeDialogProps) {
  const declareAvailability = useDeclareAvailability();
  const { notify } = useNotification();

  const form = useForm<DeclareAvailabilityFormValues>({
    resolver: zodResolver(declareAvailabilitySchema),
    defaultValues: {
      tutorId,
      startTimeLocal: initialDateKey ? `${initialDateKey}T09:00` : "",
      durationMinutes: "",
      deliveryMode: "",
    },
  });

  useEffect(() => {
    if (open) {
      form.reset({
        tutorId,
        startTimeLocal: initialDateKey ? `${initialDateKey}T09:00` : "",
        durationMinutes: "",
        deliveryMode: "",
      });
      declareAvailability.reset();
    }
    // Only re-seed when the dialog opens (or the day being pre-filled changes) — not on every keystroke.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, initialDateKey, tutorId]);

  function handleSubmit(values: DeclareAvailabilityFormValues) {
    declareAvailability.mutate(
      {
        tutorId: values.tutorId,
        startTimeUtc: fromTehranInput(values.startTimeLocal),
        duration: minutesToTimeSpan(Number(values.durationMinutes)),
        deliveryMode: Number(values.deliveryMode),
      },
      {
        onSuccess: () => {
          notify({ message: "Teaching time added.", severity: "success" });
          onClose();
        },
      },
    );
  }

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        Add Teaching Time
        <IconButton aria-label="Close" onClick={onClose} size="small">
          <CloseRoundedIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent>
        <Form form={form} onSubmit={handleSubmit}>
          <Stack spacing={2} alignItems="flex-start" width="100%" pt={1}>
            <FormTextField
              name="startTimeLocal"
              label="Start time (Tehran)"
              type="datetime-local"
              slotProps={{ inputLabel: { shrink: true } }}
              fullWidth
            />
            <FormTextField name="durationMinutes" label="Duration (minutes)" fullWidth />
            <FormSelect
              name="deliveryMode"
              label="Delivery mode"
              options={DELIVERY_MODE_OPTIONS}
              fullWidth
            />
            {declareAvailability.isError ? (
              <ErrorState error={declareAvailability.error} title="Could not add teaching time" />
            ) : null}
            <Button
              type="submit"
              variant="contained"
              fullWidth
              disabled={declareAvailability.isPending}
            >
              {declareAvailability.isPending ? "Saving…" : "Save"}
            </Button>
          </Stack>
        </Form>
      </DialogContent>
    </Dialog>
  );
}
