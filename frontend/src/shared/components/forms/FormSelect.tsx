import { MenuItem, TextField, type TextFieldProps } from "@mui/material";
import { useController, useFormContext } from "react-hook-form";

export interface FormSelectOption {
  value: string;
  label: string;
}

export interface FormSelectProps extends Omit<TextFieldProps, "name" | "error" | "select"> {
  name: string;
  options: FormSelectOption[];
}

/** A select input bound to the nearest Form's react-hook-form context by field name. */
export function FormSelect({ name, options, helperText, ...rest }: FormSelectProps) {
  const { control } = useFormContext<Record<string, unknown>>();
  const { field, fieldState } = useController({ name, control });

  return (
    <TextField
      {...rest}
      {...field}
      value={field.value ?? ""}
      select
      error={Boolean(fieldState.error)}
      helperText={fieldState.error?.message ?? helperText}
      fullWidth
    >
      {options.map((option) => (
        <MenuItem key={option.value} value={option.value}>
          {option.label}
        </MenuItem>
      ))}
    </TextField>
  );
}
