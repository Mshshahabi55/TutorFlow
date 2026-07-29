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
    expect(screen.getByText("Verified")).toBeInTheDocument();

    rerender(
      <MemoryRouter>
        <TutorCard tutor={{ ...BASE_TUTOR, isApproved: false }} />
      </MemoryRouter>,
    );
    expect(screen.queryByText("Verified")).not.toBeInTheDocument();
  });

  it("shows a real photo when photoUrl is set, and a display name over the subject fallback", () => {
    renderCard({ ...BASE_TUTOR, displayName: "Jane Doe", headline: "Friendly Math Tutor", photoUrl: "https://example.com/jane.jpg" });

    expect(screen.getByRole("heading", { name: "Jane Doe" })).toBeInTheDocument();
    expect(screen.getByText("Friendly Math Tutor")).toBeInTheDocument();
    expect(screen.getByRole("img")).toHaveAttribute("src", "https://example.com/jane.jpg");
  });

  it("shows additional subjects (with level) and other languages when present", () => {
    renderCard({
      ...BASE_TUTOR,
      tutorSubjects: [
        { subject: "Mathematics", level: null },
        { subject: "Physics", level: "Advanced" },
      ],
      otherLanguages: ["French"],
    });

    expect(screen.getByText("Physics (Advanced)")).toBeInTheDocument();
    expect(screen.getByText("French")).toBeInTheDocument();
  });

  it("shows a trial-lesson badge only when the Tutor offers one", () => {
    const { rerender } = render(
      <MemoryRouter>
        <TutorCard tutor={{ ...BASE_TUTOR, trialLessonAvailable: true }} />
      </MemoryRouter>,
    );
    expect(screen.getByText("Trial lesson available")).toBeInTheDocument();

    rerender(
      <MemoryRouter>
        <TutorCard tutor={{ ...BASE_TUTOR, trialLessonAvailable: false }} />
      </MemoryRouter>,
    );
    expect(screen.queryByText("Trial lesson available")).not.toBeInTheDocument();
  });

  it("never shows a rating, review count, or online indicator, since none exist in the API", () => {
    renderCard(BASE_TUTOR);

    expect(screen.queryByText(/review/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/rating/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/online/i)).not.toBeInTheDocument();
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

  it("shows an honest 'Learning Plans coming soon' badge, never a fabricated starting price, and links to the profile's Learning Plans section", () => {
    renderCard(BASE_TUTOR);

    expect(screen.getByText("Learning Plans coming soon")).toBeInTheDocument();
    expect(screen.queryByText(/Starting from/)).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "View Learning Plans" })).toHaveAttribute(
      "href",
      "/identity/tutors/11111111-1111-1111-1111-111111111111#learning-plans",
    );
  });
});
