import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { TutorCard } from "@/features/discovery/components/TutorCard";
import type { TutorDto } from "@/services/api/dtos";

const BASE_TUTOR: TutorDto = {
  tutorId: "11111111-1111-1111-1111-111111111111",
  isApproved: false,
  isSuspended: false,
  isDiscoverable: true,
  hourlyRate: 500_000,
  subject: "Mathematics",
  language: "English",
  location: "Remote",
  offeredDurations: ["00:30:00", "01:00:00"],
};

function renderCard(tutor: TutorDto) {
  return render(
    <MemoryRouter>
      <TutorCard tutor={tutor} />
    </MemoryRouter>,
  );
}

describe("TutorCard", () => {
  it("renders the subject as the card heading, since Tutor has no name field", () => {
    renderCard(BASE_TUTOR);

    expect(screen.getByRole("heading", { name: "Mathematics" })).toBeInTheDocument();
  });

  it("falls back to a generic heading when subject is null", () => {
    renderCard({ ...BASE_TUTOR, subject: null });

    expect(screen.getByRole("heading", { name: "Tutor" })).toBeInTheDocument();
  });

  it("shows a Verified badge only when the Tutor is approved", () => {
    const { rerender } = render(
      <MemoryRouter>
        <TutorCard tutor={{ ...BASE_TUTOR, isApproved: true }} />
      </MemoryRouter>,
    );
    expect(screen.getByTitle("Verified tutor")).toBeInTheDocument();

    rerender(
      <MemoryRouter>
        <TutorCard tutor={{ ...BASE_TUTOR, isApproved: false }} />
      </MemoryRouter>,
    );
    expect(screen.queryByTitle("Verified tutor")).not.toBeInTheDocument();
  });

  it("shows language, location, price, and offered durations from real Tutor data", () => {
    renderCard(BASE_TUTOR);

    expect(screen.getByText("Remote")).toBeInTheDocument();
    expect(screen.getByText("English")).toBeInTheDocument();
    expect(screen.getByText("50,000 Toman/hr")).toBeInTheDocument();
    expect(screen.getByText("Sessions: 30, 60 min")).toBeInTheDocument();
  });

  it("shows a 'Rate not set' placeholder when hourlyRate is null, never a fabricated price", () => {
    renderCard({ ...BASE_TUTOR, hourlyRate: null });

    expect(screen.getByText("Rate not set")).toBeInTheDocument();
  });

  it("links its secondary action to the Tutor's existing detail route", () => {
    renderCard(BASE_TUTOR);

    expect(screen.getByRole("link", { name: "View profile" })).toHaveAttribute(
      "href",
      "/identity/tutors/11111111-1111-1111-1111-111111111111",
    );
  });

  it("links its primary Book Lesson action straight into the booking wizard with this Tutor pre-selected", () => {
    renderCard(BASE_TUTOR);

    expect(screen.getByRole("link", { name: "Book Lesson" })).toHaveAttribute(
      "href",
      "/scheduling/sessions/book?tutorId=11111111-1111-1111-1111-111111111111",
    );
  });
});
