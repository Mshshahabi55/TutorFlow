import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { NotificationsButton } from "@/layouts/NotificationsButton";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { AuthHarness } from "@/test/AuthHarness";
import * as communicationService from "@/features/communication/api/communicationService";
import { NotificationType } from "@/services/api/dtos";
import type { NotificationDto } from "@/services/api/dtos";

const AUTH_USER = {
  token: "t",
  accountId: "a1",
  role: "Student",
  expiresAtUtc: "2999-01-01T00:00:00Z",
  email: "student@example.com",
};

const NOTIFICATIONS: NotificationDto[] = [
  {
    notificationId: "n1",
    type: NotificationType.BookingConfirmed,
    summary: "Your lesson has been booked.",
    relatedEntityId: "session-1",
    createdAtUtc: "2026-08-01T14:00:00Z",
    readAtUtc: null,
  },
  {
    notificationId: "n2",
    type: NotificationType.TutorReplied,
    summary: "Your tutor replied to your message.",
    relatedEntityId: "conversation-1",
    createdAtUtc: "2026-08-01T10:00:00Z",
    readAtUtc: "2026-08-01T11:00:00Z",
  },
];

function renderButton() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter>
          <AuthHarness user={AUTH_USER} />
          <NotificationsButton />
        </MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

describe("NotificationsButton", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows a real unread badge count", async () => {
    vi.spyOn(communicationService, "fetchMyNotifications").mockResolvedValue(NOTIFICATIONS);

    renderButton();

    expect(await screen.findByText("1")).toBeInTheDocument();
  });

  it("shows no badge when there are zero unread notifications", async () => {
    vi.spyOn(communicationService, "fetchMyNotifications").mockResolvedValue([]);

    renderButton();

    await userEvent.click(screen.getByRole("button", { name: "Notifications" }));
    expect(await screen.findByText("No notifications yet.")).toBeInTheDocument();
    // MUI's Badge always renders its count node; a zero count is hidden via
    // the "invisible" modifier class rather than not rendering at all.
    expect(screen.getByText("0")).toHaveClass("MuiBadge-invisible");
  });

  it("lists recent notifications in the dropdown", async () => {
    vi.spyOn(communicationService, "fetchMyNotifications").mockResolvedValue(NOTIFICATIONS);

    renderButton();
    await userEvent.click(screen.getByRole("button", { name: "Notifications" }));

    expect(await screen.findByText("Your lesson has been booked.")).toBeInTheDocument();
    expect(screen.getByText("Your tutor replied to your message.")).toBeInTheDocument();
  });

  it("marks a notification read when clicked", async () => {
    vi.spyOn(communicationService, "fetchMyNotifications").mockResolvedValue(NOTIFICATIONS);
    const markRead = vi.spyOn(communicationService, "markNotificationRead").mockResolvedValue(undefined);

    renderButton();
    await userEvent.click(screen.getByRole("button", { name: "Notifications" }));
    await userEvent.click(await screen.findByText("Your lesson has been booked."));

    expect(markRead).toHaveBeenCalledWith("n1");
  });

  it("shows Mark all read only when there is at least one unread notification, and calls the endpoint", async () => {
    vi.spyOn(communicationService, "fetchMyNotifications").mockResolvedValue(NOTIFICATIONS);
    const markAllRead = vi
      .spyOn(communicationService, "markAllNotificationsRead")
      .mockResolvedValue(undefined);

    renderButton();
    await userEvent.click(screen.getByRole("button", { name: "Notifications" }));

    const button = await screen.findByRole("button", { name: "Mark all read" });
    await userEvent.click(button);

    await waitFor(() => expect(markAllRead).toHaveBeenCalled());
  });

  it("hides Mark all read when every notification is already read", async () => {
    vi.spyOn(communicationService, "fetchMyNotifications").mockResolvedValue([
      { ...NOTIFICATIONS[1] },
    ]);

    renderButton();
    await userEvent.click(screen.getByRole("button", { name: "Notifications" }));

    await screen.findByText("Your tutor replied to your message.");
    expect(screen.queryByRole("button", { name: "Mark all read" })).not.toBeInTheDocument();
  });
});
