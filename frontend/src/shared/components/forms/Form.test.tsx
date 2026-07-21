import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { FormSelect } from "@/shared/components/forms/FormSelect";
import { FormCheckbox } from "@/shared/components/forms/FormCheckbox";
import { Button } from "@mui/material";

const schema = z.object({
  subject: z.string().min(1, "Subject is required"),
  deliveryMode: z.string().min(1, "Delivery mode is required"),
  agree: z.boolean().refine((value) => value, "You must agree"),
});

type FormValues = z.infer<typeof schema>;

function TestForm({ onSubmit }: { onSubmit: (values: FormValues) => void }) {
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { subject: "", deliveryMode: "", agree: false },
  });

  return (
    <Form form={form} onSubmit={onSubmit}>
      <FormTextField name="subject" label="Subject" />
      <FormSelect
        name="deliveryMode"
        label="Delivery mode"
        options={[
          { value: "Online", label: "Online" },
          { value: "InPerson", label: "In-person" },
        ]}
      />
      <FormCheckbox name="agree" label="I agree" />
      <Button type="submit">Submit</Button>
    </Form>
  );
}

describe("Form + FormTextField/FormSelect/FormCheckbox", () => {
  it("submits the entered values when valid", async () => {
    const onSubmit = vi.fn();
    render(<TestForm onSubmit={onSubmit} />);

    await userEvent.type(screen.getByLabelText("Subject"), "Mathematics");
    await userEvent.click(screen.getByLabelText("Delivery mode"));
    await userEvent.click(await screen.findByRole("option", { name: "Online" }));
    await userEvent.click(screen.getByLabelText("I agree"));
    await userEvent.click(screen.getByRole("button", { name: "Submit" }));

    expect(onSubmit).toHaveBeenCalledWith(
      { subject: "Mathematics", deliveryMode: "Online", agree: true },
      expect.anything(),
    );
  });

  it("shows validation errors and does not submit when invalid", async () => {
    const onSubmit = vi.fn();
    render(<TestForm onSubmit={onSubmit} />);

    await userEvent.click(screen.getByRole("button", { name: "Submit" }));

    expect(await screen.findByText("Subject is required")).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });
});
