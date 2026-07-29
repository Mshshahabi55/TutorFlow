import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ConversationListItem } from "@/features/communication/components/ConversationListItem";
import type { ConversationDto, TutorDto } from "@/services/api/dtos";

const CONVERSATION: ConversationDto = {
  conversationId: "c1",
  otherParticipantId: "t1",
  createdAtUtc: "2026-08-01T14:00:00Z",
  lastMessageAtUtc: "2026-08-01T15:00:00Z",
  lastMessagePreview: "See you Tuesday!",
  unreadCount: 0,
};

describe("ConversationListItem", () => {
  it("shows the other participant's id, last message preview, and timestamp", () => {
    render(<ConversationListItem conversation={CONVERSATION} onOpen={() => {}} />);

    expect(screen.getByText("t1")).toBeInTheDocument();
    expect(screen.getByText("See you Tuesday!")).toBeInTheDocument();
  });

  it("shows an unread badge only when unreadCount is greater than zero", () => {
    render(
      <ConversationListItem conversation={{ ...CONVERSATION, unreadCount: 3 }} onOpen={() => {}} />,
    );

    expect(screen.getByText("3 unread")).toBeInTheDocument();
  });

  it("shows a placeholder when there is no message yet", () => {
    render(
      <ConversationListItem
        conversation={{ ...CONVERSATION, lastMessagePreview: null }}
        onOpen={() => {}}
      />,
    );

    expect(screen.getByText("No messages yet")).toBeInTheDocument();
  });

  it("calls onOpen with the conversation when clicked", async () => {
    const onOpen = vi.fn();
    render(<ConversationListItem conversation={CONVERSATION} onOpen={onOpen} />);

    await userEvent.click(screen.getByText("t1"));

    expect(onOpen).toHaveBeenCalledWith(CONVERSATION);
  });

  it("shows the resolved Tutor's name and photo instead of the raw id when one is given", () => {
    const tutor: TutorDto = {
      tutorId: "t1",
      isApproved: true,
      isSuspended: false,
      isDiscoverable: true,
      hourlyRate: 500_000,
      subject: "Mathematics",
      language: "English",
      location: "Remote",
      offeredDurations: [],
      displayName: "Jane Doe",
    };

    render(<ConversationListItem conversation={CONVERSATION} onOpen={() => {}} otherParticipantTutor={tutor} />);

    expect(screen.getByText("Jane Doe")).toBeInTheDocument();
    expect(screen.queryByText("t1")).not.toBeInTheDocument();
  });
});
