import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MessageComposer } from "@/features/communication/components/MessageComposer";

describe("MessageComposer", () => {
  it("disables the send button while the input is empty or whitespace-only", () => {
    render(<MessageComposer onSend={() => {}} isSending={false} />);

    expect(screen.getByRole("button", { name: "Send message" })).toBeDisabled();
  });

  it("sends the trimmed message and clears the input when the button is clicked", async () => {
    const onSend = vi.fn();
    render(<MessageComposer onSend={onSend} isSending={false} />);

    const input = screen.getByLabelText("Message");
    await userEvent.type(input, "  Hello!  ");
    await userEvent.click(screen.getByRole("button", { name: "Send message" }));

    expect(onSend).toHaveBeenCalledWith("Hello!");
    expect(input).toHaveValue("");
  });

  it("sends on Enter and does not insert a newline", async () => {
    const onSend = vi.fn();
    render(<MessageComposer onSend={onSend} isSending={false} />);

    const input = screen.getByLabelText("Message");
    await userEvent.type(input, "Hi{Enter}");

    expect(onSend).toHaveBeenCalledWith("Hi");
  });

  it("does not send while a message is already being sent", () => {
    render(<MessageComposer onSend={() => {}} isSending={true} />);

    expect(screen.getByRole("button", { name: "Send message" })).toBeDisabled();
  });
});
