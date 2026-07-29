import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { TutorSearchPage } from "@/features/discovery/pages/TutorSearchPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as discoveryService from "@/features/discovery/api/discoveryService";

/** RC2: Language/Location/Available from live behind the Filters drawer at every viewport (TutorFilterPanel) — Subject stays on the always-visible SearchHero. */
async function openFilters() {
  await userEvent.click(screen.getByRole("button", { name: /Filters/ }));
}

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

  it("shows skeleton cards while the search is pending, with no layout shift once results arrive", () => {
    vi.spyOn(discoveryService, "searchTutors").mockReturnValue(new Promise(() => {}));

    renderWithProviders(<TutorSearchPage />);

    expect(screen.getAllByTestId("tutor-card-skeleton").length).toBeGreaterThan(0);
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

  it("shows a Clear filters action and a removable filter chip only after a filtered search, and resets both on click", async () => {
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

    expect(await screen.findByText("Subject: Mathematics")).toBeInTheDocument();

    await userEvent.click(await screen.findByRole("button", { name: "Clear filters" }));

    expect(searchTutors).toHaveBeenLastCalledWith(
      { subject: "", language: "", location: "", availableFrom: "" },
      1,
      20,
    );
    expect(screen.getByLabelText("Subject")).toHaveValue("");
    expect(screen.queryByRole("button", { name: "Clear filters" })).not.toBeInTheDocument();
    expect(screen.queryByText("Subject: Mathematics")).not.toBeInTheDocument();
  });

  it("removes a single active filter via its chip without clearing the others", async () => {
    const searchTutors = vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<TutorSearchPage />);
    await screen.findByText("No Tutors match these filters");

    await userEvent.type(screen.getByLabelText("Subject"), "Mathematics");
    await openFilters();
    await userEvent.type(screen.getByLabelText("Language"), "English");
    await userEvent.click(screen.getByRole("button", { name: "Show results" }));
    await userEvent.click(screen.getByRole("button", { name: "Search" }));

    await screen.findByText("Subject: Mathematics");
    const languageChip = screen.getByText("Language: English");
    const deleteIcon = languageChip.parentElement?.querySelector("svg");
    expect(deleteIcon).not.toBeNull();
    await userEvent.click(deleteIcon as SVGElement);

    expect(searchTutors).toHaveBeenLastCalledWith(
      { subject: "Mathematics", language: "", location: "", availableFrom: "" },
      1,
      20,
    );
    expect(screen.queryByText("Language: English")).not.toBeInTheDocument();
    expect(screen.getByText("Subject: Mathematics")).toBeInTheDocument();
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

    await openFilters();
    // 2026-08-01T17:30 Tehran (UTC+03:30) is 2026-08-01T14:00:00Z.
    await userEvent.type(screen.getByLabelText("Available from (Tehran)"), "2026-08-01T17:30");
    await userEvent.click(screen.getByRole("button", { name: "Show results" }));
    await userEvent.click(screen.getByRole("button", { name: "Search" }));

    expect(searchTutors).toHaveBeenLastCalledWith(
      { subject: "", language: "", location: "", availableFrom: "2026-08-01T14:00:00.000Z" },
      1,
      20,
    );
  });

  it("navigates to the Tutor's detail page from a card's View profile action", async () => {
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

    await screen.findByRole("heading", { name: "Mathematics" });
    await userEvent.click(screen.getByRole("link", { name: "View profile" }));

    expect(await screen.findByText("Tutor detail route reached")).toBeInTheDocument();
  });

  it("shows a Verified badge only for an approved Tutor", async () => {
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
        {
          tutorId: "22222222-2222-2222-2222-222222222222",
          isApproved: false,
          isSuspended: false,
          isDiscoverable: true,
          hourlyRate: 300_000,
          subject: "Physics",
          language: "Persian",
          location: "Tehran",
          offeredDurations: [],
        },
      ],
      totalCount: 2,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<TutorSearchPage />);

    await screen.findByRole("heading", { name: "Mathematics" });
    expect(screen.getByText("Verified")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Physics" })).toBeInTheDocument();
  });

  it("clears the typed subject via the search field's own clear button", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<TutorSearchPage />);
    await screen.findByText("No Tutors match these filters");

    expect(screen.queryByRole("button", { name: "Clear subject" })).not.toBeInTheDocument();

    await userEvent.type(screen.getByLabelText("Subject"), "Mathematics");
    expect(screen.getByLabelText("Subject")).toHaveValue("Mathematics");

    await userEvent.click(screen.getByRole("button", { name: "Clear subject" }));
    expect(screen.getByLabelText("Subject")).toHaveValue("");
  });

  it("shows a Reset Filters action on the empty state only when a filter is active", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<TutorSearchPage />);
    await screen.findByText("No Tutors match these filters");
    expect(screen.queryByRole("button", { name: "Reset Filters" })).not.toBeInTheDocument();

    await userEvent.type(screen.getByLabelText("Subject"), "Mathematics");
    await userEvent.click(screen.getByRole("button", { name: "Search" }));

    await userEvent.click(await screen.findByRole("button", { name: "Reset Filters" }));
    expect(screen.getByLabelText("Subject")).toHaveValue("");
  });

  it("offers Try again, Clear filters, and Go Home when the search fails, never the raw backend error", async () => {
    const searchTutors = vi
      .spyOn(discoveryService, "searchTutors")
      .mockRejectedValueOnce(new Error("Network Error"))
      .mockResolvedValueOnce({ items: [], totalCount: 0, page: 1, pageSize: 20 });

    renderWithProviders(<TutorSearchPage />);

    expect(await screen.findByRole("heading", { name: "We couldn't load Tutors" })).toBeInTheDocument();
    expect(screen.queryByText("Network Error")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Go Home" })).toHaveAttribute("href", "/");
    // No filter is active yet, so there is nothing to clear.
    expect(screen.queryByRole("button", { name: "Clear filters" })).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(await screen.findByText("No Tutors match these filters")).toBeInTheDocument();
    expect(searchTutors).toHaveBeenCalledTimes(2);
  });

  it("renders the filters behind a Filters button inside a slide-over Drawer", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<TutorSearchPage />);
    await screen.findByText("No Tutors match these filters");

    expect(screen.queryByLabelText("Language")).not.toBeInTheDocument();

    await openFilters();

    expect(await screen.findByLabelText("Language")).toBeInTheDocument();
    expect(screen.getByLabelText("Location")).toBeInTheDocument();
    expect(screen.getByLabelText("Available from (Tehran)")).toBeInTheDocument();
  });
});
