import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ParentGuardianDetailPage } from "@/features/identity/pages/ParentGuardianDetailPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";

const PARENT_GUARDIAN_ID = "44444444-4444-4444-4444-444444444444";

describe("ParentGuardianDetailPage", () => {
  it("shows an id-lookup form when no id is in the route", () => {
    renderWithProviders(<ParentGuardianDetailPage />);

    expect(screen.getByLabelText("Parent/Guardian id")).toBeInTheDocument();
  });

  it("navigates to the id-specific route and shows the Parent/Guardian once looked up", async () => {
    vi.spyOn(identityService, "fetchParentGuardianById").mockResolvedValue({
      parentGuardianId: PARENT_GUARDIAN_ID,
    });

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={["/identity/parent-guardians"]}>
          <Routes>
            <Route path="/identity/parent-guardians" element={<ParentGuardianDetailPage />} />
            <Route
              path="/identity/parent-guardians/:parentGuardianId"
              element={<ParentGuardianDetailPage />}
            />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    await userEvent.type(screen.getByLabelText("Parent/Guardian id"), PARENT_GUARDIAN_ID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByText(PARENT_GUARDIAN_ID)).toBeInTheDocument();
  });

  it("shows a friendly error state, never the raw backend error, when the lookup fails", async () => {
    vi.spyOn(identityService, "fetchParentGuardianById").mockRejectedValue(
      new Error("404 Not Found"),
    );

    renderWithProviders(<ParentGuardianDetailPage />, {
      initialEntries: [`/identity/parent-guardians/${PARENT_GUARDIAN_ID}`],
      routePath: "/identity/parent-guardians/:parentGuardianId",
    });

    expect(
      await screen.findByText("Parent/Guardian unavailable"),
    ).toBeInTheDocument();
    expect(screen.queryByText("404 Not Found")).not.toBeInTheDocument();
  });
});
