import { Checkbox, FormControlLabel, FormHelperText, Box } from "@mui/material";
import { useController, useFormContext } from "react-hook-form";

export interface FormCheckboxProps {
  name: string;
  label: string;
  disabled?: boolean;
}

/** A checkbox bound to the nearest Form's react-hook-form context by field name. */
export function FormCheckbox({ name, label, disabled }: FormCheckboxProps) {
  const { control } = useFormContext<Record<string, unknown>>();
  const { field, fieldState } = useController({ name, control });

  return (
    <Box>
      <FormControlLabel
        control={
          <Checkbox checked={Boolean(field.value)} onChange={field.onChange} disabled={disabled} />
        }
        label={label}
      />
      {fieldState.error ? (
        <FormHelperText error>{fieldState.error.message}</FormHelperText>
      ) : null}
    </Box>
  );
}
