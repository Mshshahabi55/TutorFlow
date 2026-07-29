import { afterEach, describe, expect, it, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "@/test/renderWithProviders";
import { InboxPage } from "@/features/communication/pages/InboxPage";
import * as communicationService from "@/features/communication/api/communicationService";
import * as identityService from "@/features/identity/api/identityService";
import type { ConversationDto, TutorDto } from "@/services/api/dtos";

const AUTH_USER = {
  token: "t",
  accountId: "a1",
  role: "Student",
  expiresAtUtc: "2999-01-01T00:00:00Z",
  email: "student@example.com",
};

const CONVERSATIONS: ConversationDto[] = [
  {
    conversationId: "c1",
    otherParticipantId: "t1",
    createdAtUtc: "2026-08-01T14:00:00Z",
    lastMessageAtUtc: "2026-08-01T15:00:00Z",
    lastMessagePreview: "See you Tuesday!",
    unreadCount: 2,
  },
  {
    conversationId: "c2",
    otherParticipantId: "t2",
    createdAtUtc: "2026-08-01T10:00:00Z",
    lastMessageAtUtc: "2026-08-01T10:05:00Z",
    lastMessagePreview: "Thanks for the lesson",
    unreadCount: 0,
  },
];

describe("InboxPage", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows every conversation returned by GET /conversations/mine", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue(CONVERSATIONS);

    renderWithProviders(<InboxPage />, { authUser: AUTH_USER });

    expect(await screen.findByText("See you Tuesday!")).toBeInTheDocument();
    expect(screen.getByText("Thanks for the lesson")).toBeInTheDocument();
    expect(screen.getByText("2 unread")).toBeInTheDocument();
  });

  it("shows an honest empty state when there are no conversations", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue([]);

    renderWithProviders(<InboxPage />, { authUser: AUTH_USER });

    expect(await screen.findByText("No conversations yet")).toBeInTheDocument();
  });

  it("filters the list by the last message's own text", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue(CONVERSATIONS);

    renderWithProviders(<InboxPage />, { authUser: AUTH_USER });
    await screen.findByText("See you Tuesday!");

    await userEvent.type(screen.getByLabelText("Search messages"), "Tuesday");

    expect(screen.getByText("See you Tuesday!")).toBeInTheDocument();
    expect(screen.queryByText("Thanks for the lesson")).not.toBeInTheDocument();
  });

  it("shows a no-match empty state when the search matches nothing", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue(CONVERSATIONS);

    renderWithProviders(<InboxPage />, { authUser: AUTH_USER });
    await screen.findByText("See you Tuesday!");

    await userEvent.type(screen.getByLabelText("Search messages"), "nonexistent-term");

    await waitFor(() => expect(screen.getByText("No matching conversations")).toBeInTheDocument());
  });

  it("shows an error state with a retry action when the fetch fails", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockRejectedValue(new Error("Network down"));

    renderWithProviders(<InboxPage />, { authUser: AUTH_USER });

    expect(await screen.findByText("Network down")).toBeInTheDocument();
  });

  it("shows the resolved Tutor's name once the other participant resolves as a Tutor", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue(CONVERSATIONS);
    vi.spyOn(identityService, "fetchTutorById").mockImplementation((id: string) =>
      id === "t1"
        ? Promise.resolve({
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
          } as TutorDto)
        : Promise.reject(new Error("not a tutor")),
    );

    renderWithProviders(<InboxPage />, { authUser: AUTH_USER });

    expect(await screen.findByText("Jane Doe")).toBeInTheDocument();
    // t2 never resolves as a Tutor — its row keeps the raw-id fallback.
    expect(screen.getByText("t2")).toBeInTheDocument();
  });

  it("matches search against a resolved Tutor's name, not only message text", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue(CONVERSATIONS);
    vi.spyOn(identityService, "fetchTutorById").mockImplementation((id: string) =>
      id === "t1"
        ? Promise.resolve({
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
          } as TutorDto)
        : Promise.reject(new Error("not a tutor")),
    );

    renderWithProviders(<InboxPage />, { authUser: AUTH_USER });
    await screen.findByText("Jane Doe");

    await userEvent.type(screen.getByLabelText("Search messages"), "Jane");

    expect(screen.getByText("Jane Doe")).toBeInTheDocument();
    expect(screen.queryByText("t2")).not.toBeInTheDocument();
  });
});
