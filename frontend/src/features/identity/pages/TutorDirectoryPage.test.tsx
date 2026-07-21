import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { TutorDirectoryPage } from "@/features/identity/pages/TutorDirectoryPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";

describe("TutorDirectoryPage", () => {
  it("shows an empty state when there are no discoverable Tutors", async () => {
    vi.spyOn(identityService, "fetchTutorDirectory").mockResolvedValue([]);

    renderWithProviders(<TutorDirectoryPage />);

    expect(await screen.findByText("No discoverable Tutors yet")).toBeInTheDocument();
  });

  it("lists discoverable Tutors and navigates to the detail page on row click", async () => {
    vi.spyOn(identityService, "fetchTutorDirectory").mockResolvedValue([
      {
        tutorId: "11111111-1111-1111-1111-111111111111",
        isApproved: true,
        isSuspended: false,
        isDiscoverable: true,
        hourlyRate: 40,
        subject: "Mathematics",
        language: "English",
        location: "Remote",
        offeredDurations: ["00:30:00"],
      },
    ]);

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={["/identity/tutors"]}>
          <Routes>
            <Route path="/identity/tutors" element={<TutorDirectoryPage />} />
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
