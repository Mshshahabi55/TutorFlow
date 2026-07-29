import { afterEach, describe, expect, it, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "@/test/renderWithProviders";
import { MeetingCard } from "@/features/meetings/components/MeetingCard";
import * as meetingService from "@/features/meetings/api/meetingService";
import { ApiRequestError } from "@/services/api/ApiRequestError";
import { ErrorType } from "@/services/api/apiTypes";
import { DeliveryMode, MeetingStatus, MeetingProviderOption, SessionStatus } from "@/services/api/dtos";
import type { MeetingDto, SessionDto } from "@/services/api/dtos";

const ONLINE_SESSION: SessionDto = {
  sessionId: "s1",
  tutorId: "t1",
  studentId: "st1",
  parentGuardianId: null,
  availabilitySlotId: "a1",
  scheduledTimeUtc: "2026-08-01T14:00:00Z",
  endTimeUtc: "2026-08-01T15:00:00Z",
  duration: "01:00:00",
  deliveryMode: DeliveryMode.Online,
  status: SessionStatus.Scheduled,
};

const IN_PERSON_SESSION: SessionDto = { ...ONLINE_SESSION, deliveryMode: DeliveryMode.InPerson };

const MEETING: MeetingDto = {
  meetingId: "m1",
  sessionId: "s1",
  provider: MeetingProviderOption.GoogleMeet,
  joinUrl: "https://meet.example.com/join/abc",
  hostUrl: "https://meet.example.com/host/abc",
  startsAtUtc: "2099-01-01T14:00:00Z",
  endsAtUtc: "2099-01-01T15:00:00Z",
  status: MeetingStatus.Scheduled,
  createdAtUtc: "2026-08-01T10:00:00Z",
  updatedAtUtc: "2026-08-01T10:00:00Z",
};

function authUser(role: string) {
  return { token: "t", accountId: "a1", role, expiresAtUtc: "2999-01-01T00:00:00Z", email: "user@example.com" };
}

describe("MeetingCard", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders nothing for an In-Person session", () => {
    renderWithProviders(<MeetingCard session={IN_PERSON_SESSION} />, { authUser: authUser("Student") });

    expect(screen.queryByText("Online Lesson")).not.toBeInTheDocument();
  });

  it("shows a Start Lesson button for the Tutor when no meeting has started yet", async () => {
    vi.spyOn(meetingService, "fetchMeetingBySession").mockRejectedValue(
      new ApiRequestError({ code: "GetMeetingBySessionQuery.NotFound", message: "not found", type: ErrorType.Domain }, 404),
    );

    renderWithProviders(<MeetingCard session={ONLINE_SESSION} />, { authUser: authUser("Tutor") });

    expect(await screen.findByRole("button", { name: "Start Lesson" })).toBeInTheDocument();
  });

  it("shows an honest waiting message for the Student when no meeting has started yet", async () => {
    vi.spyOn(meetingService, "fetchMeetingBySession").mockRejectedValue(
      new ApiRequestError({ code: "GetMeetingBySessionQuery.NotFound", message: "not found", type: ErrorType.Domain }, 404),
    );

    renderWithProviders(<MeetingCard session={ONLINE_SESSION} />, { authUser: authUser("Student") });

    expect(await screen.findByText("Lesson hasn't started yet")).toBeInTheDocument();
  });

  it("shows the provider badge and a Join Lesson link (to the join url) for the Student once a meeting exists", async () => {
    vi.spyOn(meetingService, "fetchMeetingBySession").mockResolvedValue(MEETING);

    renderWithProviders(<MeetingCard session={ONLINE_SESSION} />, { authUser: authUser("Student") });

    expect(await screen.findByText("Google Meet")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Join Lesson" })).toHaveAttribute("href", MEETING.joinUrl);
  });

  it("links the Tutor's own button to the host url", async () => {
    vi.spyOn(meetingService, "fetchMeetingBySession").mockResolvedValue(MEETING);

    renderWithProviders(<MeetingCard session={ONLINE_SESSION} />, { authUser: authUser("Tutor") });

    expect(await screen.findByRole("link", { name: "Start Lesson" })).toHaveAttribute("href", MEETING.hostUrl!);
  });

  it("labels the Parent/Guardian's action View Meeting", async () => {
    vi.spyOn(meetingService, "fetchMeetingBySession").mockResolvedValue(MEETING);

    renderWithProviders(<MeetingCard session={ONLINE_SESSION} />, { authUser: authUser("ParentGuardian") });

    expect(await screen.findByRole("link", { name: "View Meeting" })).toBeInTheDocument();
  });

  it("shows no join action for Admin — read-only", async () => {
    vi.spyOn(meetingService, "fetchMeetingBySession").mockResolvedValue(MEETING);

    renderWithProviders(<MeetingCard session={ONLINE_SESSION} />, { authUser: authUser("AdminStaff") });

    await screen.findByText("Google Meet");
    expect(screen.queryByRole("link")).not.toBeInTheDocument();
  });

  it("shows a cancelled message and no join action once the meeting is cancelled", async () => {
    vi.spyOn(meetingService, "fetchMeetingBySession").mockResolvedValue({ ...MEETING, status: MeetingStatus.Cancelled });

    renderWithProviders(<MeetingCard session={ONLINE_SESSION} />, { authUser: authUser("Student") });

    expect(await screen.findByText("This lesson’s online meeting was cancelled.", { exact: false })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Join Lesson" })).not.toBeInTheDocument();
  });

  it("shows an ended message and no join action once the meeting's own end time has passed", async () => {
    vi.spyOn(meetingService, "fetchMeetingBySession").mockResolvedValue({
      ...MEETING,
      startsAtUtc: "2020-01-01T14:00:00Z",
      endsAtUtc: "2020-01-01T15:00:00Z",
    });

    renderWithProviders(<MeetingCard session={ONLINE_SESSION} />, { authUser: authUser("Student") });

    expect(await screen.findByText("This lesson has ended.")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Join Lesson" })).not.toBeInTheDocument();
  });

  it("shows an honest Provider not configured message instead of a generic error", async () => {
    vi.spyOn(meetingService, "fetchMeetingBySession").mockRejectedValue(
      new ApiRequestError({ code: "GetMeetingBySessionQuery.NotFound", message: "not found", type: ErrorType.Domain }, 404),
    );
    vi.spyOn(meetingService, "startMeeting").mockRejectedValue(
      new ApiRequestError(
        {
          code: "CreateMeetingCommand.ProviderNotConfigured",
          message: "This meeting provider isn't configured yet.",
          type: ErrorType.Infrastructure,
        },
        500,
      ),
    );

    renderWithProviders(<MeetingCard session={ONLINE_SESSION} />, { authUser: authUser("Tutor") });

    await userEvent.click(await screen.findByRole("button", { name: "Start Lesson" }));

    await waitFor(() =>
      expect(screen.getByText(/Provider not configured/)).toBeInTheDocument(),
    );
  });
});
