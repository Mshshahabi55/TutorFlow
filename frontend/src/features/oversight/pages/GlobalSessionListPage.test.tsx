import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { GlobalSessionListPage } from "@/features/oversight/pages/GlobalSessionListPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import * as oversightService from "@/features/oversight/api/oversightService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";

const SESSION = {
  sessionId: "44444444-4444-4444-4444-444444444444",
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

describe("GlobalSessionListPage", () => {
  it("shows an empty state when no sessions have been booked", async () => {
    vi.spyOn(oversightService, "fetchAllSessions").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<GlobalSessionListPage />);

    expect(await screen.findByText("No sessions have been booked yet")).toBeInTheDocument();
  });

  it("lists sessions and navigates to the detail page on row click", async () => {
    vi.spyOn(oversightService, "fetchAllSessions").mockResolvedValue({
      items: [SESSION],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <NotificationProvider>
          <ConfirmDialogProvider>
            <MemoryRouter initialEntries={["/oversight/sessions"]}>
              <Routes>
                <Route path="/oversight/sessions" element={<GlobalSessionListPage />} />
                <Route
                  path="/scheduling/sessions/:sessionId"
                  element={<div>Session detail route reached</div>}
                />
              </Routes>
            </MemoryRouter>
          </ConfirmDialogProvider>
        </NotificationProvider>
      </QueryClientProvider>,
    );

    const cell = await screen.findByText("Tutor: t1");
    await userEvent.click(cell);

    expect(await screen.findByText("Session detail route reached")).toBeInTheDocument();
  });

  it("clicking a row action does not also navigate the row to the detail page", async () => {
    vi.spyOn(oversightService, "fetchAllSessions").mockResolvedValue({
      items: [SESSION],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <NotificationProvider>
          <ConfirmDialogProvider>
            <MemoryRouter initialEntries={["/oversight/sessions"]}>
              <Routes>
                <Route path="/oversight/sessions" element={<GlobalSessionListPage />} />
                <Route
                  path="/scheduling/sessions/:sessionId"
                  element={<div>Session detail route reached</div>}
                />
              </Routes>
            </MemoryRouter>
          </ConfirmDialogProvider>
        </NotificationProvider>
      </QueryClientProvider>,
    );

    await userEvent.click(await screen.findByRole("button", { name: "Cancel" }));

    expect(screen.getByText("Cancel this session?")).toBeInTheDocument();
    expect(screen.queryByText("Session detail route reached")).not.toBeInTheDocument();
  });

  it("shows a skeleton layout while loading, not an abrupt spinner", () => {
    vi.spyOn(oversightService, "fetchAllSessions").mockReturnValue(new Promise(() => {}));

    renderWithProviders(<GlobalSessionListPage />);

    expect(screen.getAllByTestId("admin-session-card-skeleton").length).toBeGreaterThan(0);
  });

  it("groups sessions by status, and shows both the Tutor and Student id on each card", async () => {
    const completed = { ...SESSION, sessionId: "55555555-5555-5555-5555-555555555555", status: SessionStatus.Completed };
    vi.spyOn(oversightService, "fetchAllSessions").mockResolvedValue({
      items: [SESSION, completed],
      totalCount: 2,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<GlobalSessionListPage />);

    expect(await screen.findByRole("heading", { name: "Upcoming" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Completed" })).toBeInTheDocument();
    expect(screen.getAllByText("Tutor: t1")).toHaveLength(2);
    expect(screen.getAllByText("Student: st1")).toHaveLength(2);
  });
});
