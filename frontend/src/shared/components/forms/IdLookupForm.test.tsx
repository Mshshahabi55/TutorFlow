import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";

const VALID_GUID = "11111111-1111-1111-1111-111111111111";

describe("IdLookupForm", () => {
  it("calls onSubmit with the entered id when it is a valid GUID", async () => {
    const onSubmit = vi.fn();
    render(<IdLookupForm label="Tutor id" onSubmit={onSubmit} />);

    await userEvent.type(screen.getByLabelText("Tutor id"), VALID_GUID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(onSubmit).toHaveBeenCalledWith(VALID_GUID);
  });

  it("shows a validation error and does not submit for a malformed id", async () => {
    const onSubmit = vi.fn();
    render(<IdLookupForm label="Tutor id" onSubmit={onSubmit} />);

    await userEvent.type(screen.getByLabelText("Tutor id"), "not-a-guid");
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByText(/enter a valid id/i)).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });
});
