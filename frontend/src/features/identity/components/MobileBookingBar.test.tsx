import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { MobileBookingBar } from "@/features/identity/components/MobileBookingBar";
import type { TutorDto } from "@/services/api/dtos";

const TUTOR: TutorDto = {
  tutorId: "11111111-1111-1111-1111-111111111111",
  isApproved: true,
  isSuspended: false,
  isDiscoverable: true,
  hourlyRate: 500_000,
  subject: "Mathematics",
  language: "English",
  location: "Remote",
  offeredDurations: [],
};

describe("MobileBookingBar", () => {
  it("shows the Tutor's real hourly rate and a Book Lesson link into the existing booking route", () => {
    render(
      <MemoryRouter>
        <MobileBookingBar tutor={TUTOR} />
      </MemoryRouter>,
    );

    expect(screen.getByText("50,000 Toman/hr")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Book Lesson" })).toHaveAttribute(
      "href",
      "/scheduling/sessions/book?tutorId=11111111-1111-1111-1111-111111111111",
    );
  });

  it("shows a 'Rate not set' placeholder when hourlyRate is null, never a fabricated price", () => {
    render(
      <MemoryRouter>
        <MobileBookingBar tutor={{ ...TUTOR, hourlyRate: null }} />
      </MemoryRouter>,
    );

    expect(screen.getByText("Rate not set")).toBeInTheDocument();
  });
});
