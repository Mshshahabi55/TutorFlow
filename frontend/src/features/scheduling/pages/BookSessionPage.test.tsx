import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BookSessionPage } from "@/features/scheduling/pages/BookSessionPage";
import { renderWithProviders } from "@/test/renderWithProviders";
import * as schedulingService from "@/features/scheduling/api/schedulingService";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";

const SLOT_ID = "22222222-2222-2222-2222-222222222222";
const STUDENT_ID = "33333333-3333-3333-3333-333333333333";
const SESSION_ID = "44444444-4444-4444-4444-444444444444";

describe("BookSessionPage", () => {
  it("pre-fills the Availability Slot id from the query string", () => {
    renderWithProviders(<BookSessionPage />, {
      initialEntries: [`/scheduling/sessions/book?availabilitySlotId=${SLOT_ID}`],
    });

    expect(screen.getByLabelText("Availability Slot id")).toHaveValue(SLOT_ID);
  });

  it("books a session with parentGuardianId as null when left blank", async () => {
    const bookSession = vi.spyOn(schedulingService, "bookSession").mockResolvedValue({
      sessionId: SESSION_ID,
      tutorId: "t1",
      studentId: STUDENT_ID,
      parentGuardianId: null,
      availabilitySlotId: SLOT_ID,
      scheduledTimeUtc: "2026-08-01T14:00:00Z",
      endTimeUtc: "2026-08-01T15:00:00Z",
      duration: "01:00:00",
      deliveryMode: DeliveryMode.Online,
      status: SessionStatus.Scheduled,
    });

    renderWithProviders(<BookSessionPage />);

    await userEvent.type(screen.getByLabelText("Availability Slot id"), SLOT_ID);
    await userEvent.type(screen.getByLabelText("Student id"), STUDENT_ID);
    await userEvent.click(screen.getByRole("button", { name: "Book session" }));

    expect(bookSession).toHaveBeenCalledWith({
      availabilitySlotId: SLOT_ID,
      studentId: STUDENT_ID,
      parentGuardianId: null,
    });
    expect(await screen.findByText(SESSION_ID)).toBeInTheDocument();
  });
});
