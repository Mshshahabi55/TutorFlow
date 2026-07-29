import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MessageBubble } from "@/features/communication/components/MessageBubble";
import type { MessageDto } from "@/services/api/dtos";

const MESSAGE: MessageDto = {
  messageId: "m1",
  conversationId: "c1",
  senderId: "s1",
  recipientId: "r1",
  body: "Hello there",
  sentAtUtc: "2026-08-01T14:00:00Z",
  readAtUtc: null,
};

describe("MessageBubble", () => {
  it("shows the message body", () => {
    render(<MessageBubble message={MESSAGE} isOwn={false} />);

    expect(screen.getByText("Hello there")).toBeInTheDocument();
  });

  it("shows a 'Sent' delivery state for an own message that hasn't been read yet", () => {
    render(<MessageBubble message={MESSAGE} isOwn={true} />);

    expect(screen.getByLabelText("Sent")).toBeInTheDocument();
  });

  it("shows a 'Read' delivery state for an own message once readAtUtc is set", () => {
    render(<MessageBubble message={{ ...MESSAGE, readAtUtc: "2026-08-01T14:05:00Z" }} isOwn={true} />);

    expect(screen.getByLabelText("Read")).toBeInTheDocument();
  });

  it("shows no delivery state for a message the viewer received", () => {
    render(<MessageBubble message={{ ...MESSAGE, readAtUtc: "2026-08-01T14:05:00Z" }} isOwn={false} />);

    expect(screen.queryByLabelText("Read")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Sent")).not.toBeInTheDocument();
  });
});
