import { afterEach, describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "@/test/renderWithProviders";
import { JoinLessonBanner } from "@/features/meetings/components/JoinLessonBanner";
import * as meetingService from "@/features/meetings/api/meetingService";
import { MeetingProviderOption, MeetingStatus } from "@/services/api/dtos";
import type { MeetingDto } from "@/services/api/dtos";

const AUTH_USER = {
  token: "t",
  accountId: "a1",
  role: "Student",
  expiresAtUtc: "2999-01-01T00:00:00Z",
  email: "student@example.com",
};

const UPCOMING_MEETING: MeetingDto = {
  meetingId: "m1",
  sessionId: "s1",
  provider: MeetingProviderOption.Zoom,
  joinUrl: "https://zoom.example.com/j/123",
  hostUrl: "https://zoom.example.com/s/123",
  startsAtUtc: "2099-01-01T14:00:00Z",
  endsAtUtc: "2099-01-01T15:00:00Z",
  status: MeetingStatus.Scheduled,
  createdAtUtc: "2026-08-01T10:00:00Z",
  updatedAtUtc: "2026-08-01T10:00:00Z",
};

describe("JoinLessonBanner", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders nothing when there is no active meeting", async () => {
    vi.spyOn(meetingService, "fetchActiveMeetingForConversation").mockResolvedValue(null);

    const { container } = renderWithProviders(<JoinLessonBanner conversationId="c1" />, { authUser: AUTH_USER });

    await vi.waitFor(() => expect(meetingService.fetchActiveMeetingForConversation).toHaveBeenCalled());
    expect(container).toBeEmptyDOMElement();
  });

  it("shows a Join Lesson link once an active meeting exists", async () => {
    vi.spyOn(meetingService, "fetchActiveMeetingForConversation").mockResolvedValue(UPCOMING_MEETING);

    renderWithProviders(<JoinLessonBanner conversationId="c1" />, { authUser: AUTH_USER });

    expect(await screen.findByRole("link", { name: "Join Lesson" })).toHaveAttribute(
      "href",
      UPCOMING_MEETING.joinUrl,
    );
  });

  it("renders nothing once the meeting is cancelled", async () => {
    vi.spyOn(meetingService, "fetchActiveMeetingForConversation").mockResolvedValue({
      ...UPCOMING_MEETING,
      status: MeetingStatus.Cancelled,
    });

    const { container } = renderWithProviders(<JoinLessonBanner conversationId="c1" />, { authUser: AUTH_USER });

    await vi.waitFor(() => expect(meetingService.fetchActiveMeetingForConversation).toHaveBeenCalled());
    expect(container).toBeEmptyDOMElement();
  });
});
