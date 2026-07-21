import { TextField, type TextFieldProps } from "@mui/material";
import { useController, useFormContext } from "react-hook-form";

export interface FormTextFieldProps extends Omit<TextFieldProps, "name" | "error"> {
  name: string;
}

/** A TextField bound to the nearest Form's react-hook-form context by field name. */
export function FormTextField({ name, helperText, ...rest }: FormTextFieldProps) {
  const { control } = useFormContext<Record<string, unknown>>();
  const { field, fieldState } = useController({ name, control });

  return (
    <TextField
      {...rest}
      {...field}
      value={field.value ?? ""}
      error={Boolean(fieldState.error)}
      helperText={fieldState.error?.message ?? helperText}
      fullWidth
    />
  );
}
