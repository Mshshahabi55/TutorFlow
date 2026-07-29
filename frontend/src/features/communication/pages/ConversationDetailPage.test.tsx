import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "@/test/renderWithProviders";
import { ConversationDetailPage } from "@/features/communication/pages/ConversationDetailPage";
import * as communicationService from "@/features/communication/api/communicationService";
import * as identityService from "@/features/identity/api/identityService";
import * as meetingService from "@/features/meetings/api/meetingService";
import type { ConversationDto, MessageDto } from "@/services/api/dtos";

const CONVERSATION: ConversationDto = {
  conversationId: "c1",
  otherParticipantId: "t1",
  createdAtUtc: "2026-08-01T14:00:00Z",
  lastMessageAtUtc: "2026-08-01T15:00:00Z",
  lastMessagePreview: "See you Tuesday!",
  unreadCount: 1,
};

const MESSAGES: MessageDto[] = [
  {
    messageId: "m1",
    conversationId: "c1",
    senderId: "student-1",
    recipientId: "t1",
    body: "Are you free Tuesday?",
    sentAtUtc: "2026-08-01T14:00:00Z",
    readAtUtc: null,
  },
  {
    messageId: "m2",
    conversationId: "c1",
    senderId: "t1",
    recipientId: "student-1",
    body: "See you Tuesday!",
    sentAtUtc: "2026-08-01T15:00:00Z",
    readAtUtc: null,
  },
];

const AUTH_USER = {
  token: "t",
  accountId: "student-1",
  role: "Student",
  expiresAtUtc: "2999-01-01T00:00:00Z",
  email: "student@example.com",
};

function renderPage() {
  return renderWithProviders(<ConversationDetailPage />, {
    initialEntries: ["/messages/c1"],
    routePath: "/messages/:conversationId",
    authUser: AUTH_USER,
  });
}

describe("ConversationDetailPage", () => {
  // RC5.3: every ConversationDetailPage render now also renders
  // JoinLessonBanner — mocked once here since none of these tests are
  // about the Join Lesson linkage itself.
  beforeEach(() => {
    vi.spyOn(meetingService, "fetchActiveMeetingForConversation").mockResolvedValue(null);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows every message in the conversation, oldest first, and the tutor's subject as the heading", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue([CONVERSATION]);
    vi.spyOn(communicationService, "fetchConversationMessages").mockResolvedValue(MESSAGES);
    vi.spyOn(communicationService, "markConversationRead").mockResolvedValue(undefined);
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
      tutorId: "t1",
      isApproved: true,
      isSuspended: false,
      isDiscoverable: true,
      hourlyRate: 500_000,
      subject: "Mathematics",
      language: "English",
      location: "Remote",
      offeredDurations: [],
    });

    renderPage();

    expect(await screen.findByRole("heading", { name: "Mathematics" })).toBeInTheDocument();
    expect(await screen.findByText("Are you free Tuesday?")).toBeInTheDocument();
    expect(screen.getByText("See you Tuesday!")).toBeInTheDocument();
  });

  it("marks the conversation read once when opened", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue([CONVERSATION]);
    vi.spyOn(communicationService, "fetchConversationMessages").mockResolvedValue(MESSAGES);
    const markRead = vi.spyOn(communicationService, "markConversationRead").mockResolvedValue(undefined);
    vi.spyOn(identityService, "fetchTutorById").mockRejectedValue(new Error("not a tutor"));

    renderPage();

    await waitFor(() => expect(markRead).toHaveBeenCalledWith("c1"));
    expect(markRead).toHaveBeenCalledTimes(1);
  });

  it("falls back to the raw participant id when it does not resolve to a Tutor", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue([CONVERSATION]);
    vi.spyOn(communicationService, "fetchConversationMessages").mockResolvedValue([]);
    vi.spyOn(communicationService, "markConversationRead").mockResolvedValue(undefined);
    vi.spyOn(identityService, "fetchTutorById").mockRejectedValue(new Error("not found"));

    renderPage();

    expect(await screen.findByRole("heading", { name: "t1" })).toBeInTheDocument();
    expect(screen.getByText("No messages yet")).toBeInTheDocument();
  });

  it("sends a message via the composer", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue([CONVERSATION]);
    vi.spyOn(communicationService, "fetchConversationMessages").mockResolvedValue([]);
    vi.spyOn(communicationService, "markConversationRead").mockResolvedValue(undefined);
    vi.spyOn(identityService, "fetchTutorById").mockRejectedValue(new Error("not found"));
    const sendMessage = vi.spyOn(communicationService, "sendMessage").mockResolvedValue(MESSAGES[0]);

    renderPage();
    await screen.findByText("No messages yet");

    await userEvent.type(screen.getByLabelText("Message"), "Hello{Enter}");

    await waitFor(() => expect(sendMessage).toHaveBeenCalledWith("c1", "Hello"));
  });
});
