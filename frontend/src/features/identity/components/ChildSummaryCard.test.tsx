import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { ChildSummaryCard } from "@/features/identity/components/ChildSummaryCard";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import * as identityService from "@/features/identity/api/identityService";
import { DeliveryMode, RelationshipStatus, SessionStatus } from "@/services/api/dtos";
import type { RelationshipDto } from "@/services/api/dtos";

const RELATIONSHIP: RelationshipDto = {
  relationshipId: "r1",
  parentGuardianId: "pg1",
  studentId: "st1",
  status: RelationshipStatus.Confirmed,
};

function renderCard(relationship: RelationshipDto) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <NotificationProvider>
        <MemoryRouter>
          <ChildSummaryCard relationship={relationship} />
        </MemoryRouter>
      </NotificationProvider>
    </QueryClientProvider>,
  );
}

describe("ChildSummaryCard", () => {
  it("shows an Invited badge, no schedule detail, and a Confirm action for an unconfirmed relationship", async () => {
    const confirmRelationship = vi
      .spyOn(identityService, "confirmRelationship")
      .mockResolvedValue(undefined);

    renderCard({ ...RELATIONSHIP, status: RelationshipStatus.Invited });

    expect(screen.getByText("st1")).toBeInTheDocument();
    expect(screen.getByText("Invited")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /View Lessons/ })).not.toBeInTheDocument();
    expect(screen.getByText(/waiting for this relationship to be confirmed/i)).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Confirm" }));

    expect(confirmRelationship).toHaveBeenCalledWith("r1");
  });

  it("shows a Confirmed badge, next/recent lesson, current tutor, and View Lessons/Book Lesson actions", async () => {
    vi.spyOn(schedulingService, "fetchStudentSchedule").mockResolvedValue([
      {
        sessionId: "s-next",
        tutorId: "t1",
        studentId: "st1",
        parentGuardianId: null,
        availabilitySlotId: "a1",
        scheduledTimeUtc: "2026-08-05T14:00:00Z",
        endTimeUtc: "2026-08-05T15:00:00Z",
        duration: "01:00:00",
        deliveryMode: DeliveryMode.Online,
        status: SessionStatus.Scheduled,
      },
      {
        sessionId: "s-past",
        tutorId: "t1",
        studentId: "st1",
        parentGuardianId: null,
        availabilitySlotId: "a2",
        scheduledTimeUtc: "2026-07-01T14:00:00Z",
        endTimeUtc: "2026-07-01T15:00:00Z",
        duration: "01:00:00",
        deliveryMode: DeliveryMode.Online,
        status: SessionStatus.Completed,
      },
    ]);
    vi.spyOn(identityService, "fetchTutorById").mockResolvedValue({
      tutorId: "t1",
      isApproved: true,
      isSuspended: false,
      isDiscoverable: true,
      hourlyRate: 500_000,
      subject: "Mathematics",
      language: "English",
      location: "Remote",
      offeredDurations: ["01:00:00"],
    });

    renderCard(RELATIONSHIP);

    expect(screen.getByText("Confirmed")).toBeInTheDocument();
    expect(await screen.findByText("Mathematics")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /View Lessons/ })).toHaveAttribute(
      "href",
      "/scheduling/students/st1/schedule",
    );
    expect(screen.getByRole("link", { name: /Book Lesson/ })).toHaveAttribute(
      "href",
      "/scheduling/sessions/book?tutorId=t1",
    );
  });

  it("shows honest placeholders when a Confirmed child has no lessons yet", async () => {
    vi.spyOn(schedulingService, "fetchStudentSchedule").mockResolvedValue([]);

    renderCard(RELATIONSHIP);

    expect(await screen.findByText("None scheduled")).toBeInTheDocument();
    expect(screen.getByText("No lessons yet")).toBeInTheDocument();
    expect(screen.getByText("No tutor yet")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Book Lesson/ })).toHaveAttribute(
      "href",
      "/scheduling/sessions/book",
    );
  });
});
