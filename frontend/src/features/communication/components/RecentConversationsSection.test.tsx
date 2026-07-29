import { afterEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "@/test/renderWithProviders";
import { RecentConversationsSection } from "@/features/communication/components/RecentConversationsSection";
import * as communicationService from "@/features/communication/api/communicationService";
import type { ConversationDto } from "@/services/api/dtos";

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
    lastMessagePreview: "Thanks!",
    unreadCount: 0,
  },
];

describe("RecentConversationsSection", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows the total unread count across every conversation", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue(CONVERSATIONS);

    renderWithProviders(<RecentConversationsSection />, { authUser: AUTH_USER });

    // Both the section-level total badge and conversation c1's own
    // per-row badge happen to read "2 unread" here (2 total, all on one
    // conversation) — asserting the count (2), not uniqueness, is what
    // this test is actually about.
    expect(await screen.findAllByText("2 unread")).toHaveLength(2);
  });

  it("shows no unread badge when nothing is unread", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue(
      CONVERSATIONS.map((c) => ({ ...c, unreadCount: 0 })),
    );

    renderWithProviders(<RecentConversationsSection />, { authUser: AUTH_USER });

    await screen.findByText("See you Tuesday!");
    expect(screen.queryByText(/unread/)).not.toBeInTheDocument();
  });

  it("shows an honest empty state with no conversations", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue([]);

    renderWithProviders(<RecentConversationsSection />, { authUser: AUTH_USER });

    expect(await screen.findByText("No conversations yet")).toBeInTheDocument();
  });

  it("links View all to the Inbox", async () => {
    vi.spyOn(communicationService, "fetchMyConversations").mockResolvedValue([]);

    renderWithProviders(<RecentConversationsSection />, { authUser: AUTH_USER });

    expect(await screen.findByRole("link", { name: "View all" })).toHaveAttribute("href", "/messages");
  });
});
