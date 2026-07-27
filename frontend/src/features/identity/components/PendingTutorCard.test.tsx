import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import { PendingTutorCard } from "@/features/identity/components/PendingTutorCard";
import type { TutorDto } from "@/services/api/dtos";

const TUTOR: TutorDto = {
  tutorId: "11111111-1111-1111-1111-111111111111",
  isApproved: false,
  isSuspended: false,
  isDiscoverable: false,
  hourlyRate: 500_000,
  subject: "Mathematics",
  language: "English",
  location: "Remote",
  offeredDurations: [],
};

function renderCard(tutor: TutorDto) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <NotificationProvider>
        <ConfirmDialogProvider>
          <PendingTutorCard tutor={tutor} />
        </ConfirmDialogProvider>
      </NotificationProvider>
    </QueryClientProvider>,
  );
}

describe("PendingTutorCard", () => {
  it("shows the subject as heading, a Pending approval badge, and real Tutor fields", () => {
    renderCard(TUTOR);

    expect(screen.getByText("Mathematics")).toBeInTheDocument();
    expect(screen.getByText("Pending approval")).toBeInTheDocument();
    expect(screen.getByText("Speaks English")).toBeInTheDocument();
    expect(screen.getByText("Remote")).toBeInTheDocument();
    expect(screen.getByText("50,000 Toman/hr")).toBeInTheDocument();
  });

  it("shows a Suspended badge only when the Tutor is suspended", () => {
    renderCard({ ...TUTOR, isSuspended: true });

    expect(screen.getByText("Suspended")).toBeInTheDocument();
  });

  it("renders the Approve and Suspend actions unchanged", () => {
    renderCard(TUTOR);

    expect(screen.getByRole("button", { name: "Approve" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Suspend" })).toBeInTheDocument();
  });
});
