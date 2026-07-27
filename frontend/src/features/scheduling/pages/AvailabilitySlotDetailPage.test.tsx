import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AvailabilitySlotDetailPage } from "@/features/scheduling/pages/AvailabilitySlotDetailPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { DeliveryMode } from "@/services/api/dtos";

const SLOT_ID = "22222222-2222-2222-2222-222222222222";

describe("AvailabilitySlotDetailPage", () => {
  it("shows an id-lookup form when no id is in the route", () => {
    renderWithProviders(<AvailabilitySlotDetailPage />);

    expect(screen.getByLabelText("Availability Slot id")).toBeInTheDocument();
  });

  it("navigates to the id-specific route and shows the slot once looked up", async () => {
    vi.spyOn(schedulingService, "fetchAvailabilitySlotById").mockResolvedValue({
      availabilitySlotId: SLOT_ID,
      tutorId: "t1",
      startTimeUtc: "2026-08-01T14:00:00Z",
      endTimeUtc: "2026-08-01T15:00:00Z",
      duration: "01:00:00",
      deliveryMode: DeliveryMode.Online,
      isConsumed: false,
    });

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={["/scheduling/availability"]}>
          <Routes>
            <Route path="/scheduling/availability" element={<AvailabilitySlotDetailPage />} />
            <Route
              path="/scheduling/availability/:availabilitySlotId"
              element={<AvailabilitySlotDetailPage />}
            />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    await userEvent.type(screen.getByLabelText("Availability Slot id"), SLOT_ID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByText("Online")).toBeInTheDocument();
    expect(screen.getByText("60 minutes")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Book this slot" })).toHaveAttribute(
      "href",
      `/scheduling/sessions/book?availabilitySlotId=${SLOT_ID}`,
    );
  });

  it("shows a friendly error state, never the raw backend error, when the lookup fails", async () => {
    vi.spyOn(schedulingService, "fetchAvailabilitySlotById").mockRejectedValue(
      new Error("404 Not Found"),
    );

    renderWithProviders(<AvailabilitySlotDetailPage />, {
      initialEntries: [`/scheduling/availability/${SLOT_ID}`],
      routePath: "/scheduling/availability/:availabilitySlotId",
    });

    expect(
      await screen.findByText("We couldn’t find that Availability Slot"),
    ).toBeInTheDocument();
    expect(screen.queryByText("404 Not Found")).not.toBeInTheDocument();
  });
});
