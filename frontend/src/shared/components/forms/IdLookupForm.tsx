import { Button, Stack } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { idLookupSchema, type IdLookupFormValues } from "@/shared/validation/guid";

export interface IdLookupFormProps {
  label: string;
  onSubmit: (id: string) => void;
}

/**
 * Reused by every detail page across every feature module (Tutor, Student,
 * Parent/Guardian, Relationship — Sprint 6; Availability Slot, Session —
 * Sprint 7) — no capability exists to look a record up by name, only by id
 * (docs/api/API_SPECIFICATION.md §6), so every "find a record" screen
 * shares this one form rather than duplicating it per page or per feature.
 */
export function IdLookupForm({ label, onSubmit }: IdLookupFormProps) {
  const form = useForm<IdLookupFormValues>({
    resolver: zodResolver(idLookupSchema),
    defaultValues: { id: "" },
  });

  return (
    <Form form={form} onSubmit={(values) => onSubmit(values.id)}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems="flex-start">
        <FormTextField name="id" label={label} placeholder="00000000-0000-0000-0000-000000000000" />
        <Button type="submit" variant="contained" sx={{ flexShrink: 0 }}>
          Look up
        </Button>
      </Stack>
    </Form>
  );
}
