import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { TutorSearchPage } from "@/features/discovery/pages/TutorSearchPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as discoveryService from "@/features/discovery/api/discoveryService";

describe("TutorSearchPage", () => {
  it("shows an empty state when no Tutors match", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<TutorSearchPage />);

    expect(await screen.findByText("No Tutors match these filters")).toBeInTheDocument();
  });

  it("searches with the entered subject filter and resets to page 1", async () => {
    const searchTutors = vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<TutorSearchPage />);
    await screen.findByText("No Tutors match these filters");

    await userEvent.type(screen.getByLabelText("Subject"), "Mathematics");
    await userEvent.click(screen.getByRole("button", { name: "Search" }));

    expect(searchTutors).toHaveBeenLastCalledWith(
      { subject: "Mathematics", language: "", location: "", availableFrom: "" },
      1,
      20,
    );
  });

  it("shows a Clear filters action only after a filtered search, and resets it on click", async () => {
    const searchTutors = vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<TutorSearchPage />);
    await screen.findByText("No Tutors match these filters");

    expect(screen.queryByRole("button", { name: "Clear filters" })).not.toBeInTheDocument();

    await userEvent.type(screen.getByLabelText("Subject"), "Mathematics");
    await userEvent.click(screen.getByRole("button", { name: "Search" }));

    await userEvent.click(await screen.findByRole("button", { name: "Clear filters" }));

    expect(searchTutors).toHaveBeenLastCalledWith(
      { subject: "", language: "", location: "", availableFrom: "" },
      1,
      20,
    );
    expect(screen.getByLabelText("Subject")).toHaveValue("");
    expect(screen.queryByRole("button", { name: "Clear filters" })).not.toBeInTheDocument();
  });

  // Phase 3.5 replaced the free-typed UTC ISO-string filter with a native
  // `<input type="datetime-local">` (Phase 3 Task 2's pattern), so a user
  // can no longer type an arbitrary malformed string into it. This inverts
  // the old "rejects a malformed availableFrom filter" assertion into
  // "searches with a Tehran-entered availableFrom, converted to UTC".
  it("searches with a Tehran-entered availableFrom filter, converted to UTC", async () => {
    const searchTutors = vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<TutorSearchPage />);
    await screen.findByText("No Tutors match these filters");

    // 2026-08-01T17:30 Tehran (UTC+03:30) is 2026-08-01T14:00:00Z.
    await userEvent.type(screen.getByLabelText("Available from (Tehran)"), "2026-08-01T17:30");
    await userEvent.click(screen.getByRole("button", { name: "Search" }));

    expect(searchTutors).toHaveBeenLastCalledWith(
      { subject: "", language: "", location: "", availableFrom: "2026-08-01T14:00:00.000Z" },
      1,
      20,
    );
  });

  it("navigates to the Tutor's detail page on row click", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [
        {
          tutorId: "11111111-1111-1111-1111-111111111111",
          isApproved: true,
          isSuspended: false,
          isDiscoverable: true,
          hourlyRate: 500_000,
          subject: "Mathematics",
          language: "English",
          location: "Remote",
          offeredDurations: [],
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={["/discovery/tutors/search"]}>
          <Routes>
            <Route path="/discovery/tutors/search" element={<TutorSearchPage />} />
            <Route
              path="/identity/tutors/:tutorId"
              element={<div>Tutor detail route reached</div>}
            />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    const row = await screen.findByText("Mathematics");
    await userEvent.click(row);

    expect(await screen.findByText("Tutor detail route reached")).toBeInTheDocument();
  });
});
