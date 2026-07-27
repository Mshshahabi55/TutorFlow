import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { TutorSearchPage } from "@/features/discovery/pages/TutorSearchPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as discoveryService from "@/features/discovery/api/discoveryService";

// Phase 3 Step 2: the filter fields (Language, Location, Available from)
// render inline on desktop but inside a closed Drawer below the `md`
// breakpoint (TutorFilterPanel) — same convention AppLayout.test.tsx
// already established for its own responsive nav. Every test that reaches
// those fields directly forces desktop so they're present in the DOM
// without needing to open the Drawer first; the Drawer itself is exercised
// by its own dedicated test below.
function mockViewport(matches: boolean) {
  window.matchMedia = vi.fn((query: string): MediaQueryList => ({
    matches,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }));
}

describe("TutorSearchPage", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("shows an empty state when no Tutors match", async () => {
    mockViewport(true);
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
    mockViewport(true);
    vi.spyOn(discoveryService, "searchTutors").mockReturnValue(new Promise(() => {}));

    renderWithProviders(<TutorSearchPage />);

    expect(screen.getAllByTestId("tutor-card-skeleton").length).toBeGreaterThan(0);
  });

  it("searches with the entered subject filter and resets to page 1", async () => {
    mockViewport(true);
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
    mockViewport(true);
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
    mockViewport(true);
    const searchTutors = vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<TutorSearchPage />);
    await screen.findByText("No Tutors match these filters");

    await userEvent.type(screen.getByLabelText("Subject"), "Mathematics");
    await userEvent.type(screen.getByLabelText("Language"), "English");
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
    mockViewport(true);
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

  it("navigates to the Tutor's detail page from a card's View profile action", async () => {
    mockViewport(true);
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
    mockViewport(true);
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
    expect(screen.getByTitle("Verified tutor")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Physics" })).toBeInTheDocument();
  });

  it("renders the filters behind a Filters button inside a Drawer on a mobile-sized viewport", async () => {
    mockViewport(false);
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    renderWithProviders(<TutorSearchPage />);
    await screen.findByText("No Tutors match these filters");

    expect(screen.queryByLabelText("Language")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Filters" }));

    expect(await screen.findByLabelText("Language")).toBeInTheDocument();
    expect(screen.getByLabelText("Location")).toBeInTheDocument();
    expect(screen.getByLabelText("Available from (Tehran)")).toBeInTheDocument();
  });
});
