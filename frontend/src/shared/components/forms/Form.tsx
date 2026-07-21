import { FormProvider, type FieldValues, type UseFormReturn } from "react-hook-form";
import type { ReactNode } from "react";

export interface FormProps<TFieldValues extends FieldValues> {
  form: UseFormReturn<TFieldValues>;
  onSubmit: (values: TFieldValues) => void | Promise<void>;
  children: ReactNode;
  id?: string;
}

/**
 * Combines react-hook-form's FormProvider with the native &lt;form&gt;
 * submit wiring, so every feature form follows the same shape: build a
 * schema with Zod, resolve it with zodResolver, then render this wrapper
 * around FormTextField/FormSelect/FormCheckbox children.
 */
export function Form<TFieldValues extends FieldValues>({
  form,
  onSubmit,
  children,
  id,
}: FormProps<TFieldValues>) {
  return (
    <FormProvider {...form}>
      <form id={id} onSubmit={(event) => void form.handleSubmit(onSubmit)(event)} noValidate>
        {children}
      </form>
    </FormProvider>
  );
}
