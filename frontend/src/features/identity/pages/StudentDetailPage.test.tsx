import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { StudentDetailPage } from "@/features/identity/pages/StudentDetailPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as identityService from "@/features/identity/api/identityService";

const STUDENT_ID = "22222222-2222-2222-2222-222222222222";

describe("StudentDetailPage", () => {
  it("shows an id-lookup form when no id is in the route", () => {
    renderWithProviders(<StudentDetailPage />);

    expect(screen.getByLabelText("Student id")).toBeInTheDocument();
  });

  it("navigates to the id-specific route and shows the Student once looked up", async () => {
    vi.spyOn(identityService, "fetchStudentById").mockResolvedValue({
      studentId: STUDENT_ID,
      isMinor: true,
    });

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={["/identity/students"]}>
          <Routes>
            <Route path="/identity/students" element={<StudentDetailPage />} />
            <Route path="/identity/students/:studentId" element={<StudentDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    await userEvent.type(screen.getByLabelText("Student id"), STUDENT_ID);
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByText("Minor")).toBeInTheDocument();
    expect(screen.getByText(STUDENT_ID)).toBeInTheDocument();
  });

  it("shows an error state when the lookup fails", async () => {
    vi.spyOn(identityService, "fetchStudentById").mockRejectedValue(new Error("Not found"));

    renderWithProviders(<StudentDetailPage />, {
      initialEntries: [`/identity/students/${STUDENT_ID}`],
      routePath: "/identity/students/:studentId",
    });

    expect(await screen.findByText("Not found")).toBeInTheDocument();
  });
});
