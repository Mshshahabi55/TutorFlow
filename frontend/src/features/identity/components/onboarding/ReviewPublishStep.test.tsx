import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { ReviewPublishStep } from "@/features/identity/components/onboarding/ReviewPublishStep";
import type { TutorDto } from "@/services/api/dtos";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";

const BASE_TUTOR: TutorDto = {
  tutorId: TUTOR_ID,
  isApproved: false,
  isSuspended: false,
  isDiscoverable: false,
  hourlyRate: null,
  subject: null,
  language: null,
  location: null,
  offeredDurations: [],
  profileStatus: "Draft",
  displayName: null,
  headline: null,
  biography: null,
  country: null,
  city: null,
  otherLanguages: [],
  tutorSubjects: [],
  yearsOfExperience: null,
  education: null,
  certifications: null,
  teachingMethodology: null,
  lessonSpecialties: [],
  photoUrl: null,
  introVideoUrl: null,
  galleryImageUrls: [],
  trialLessonAvailable: false,
  trialLessonPrice: null,
};

function renderStep(tutor: TutorDto, overrides: Partial<Parameters<typeof ReviewPublishStep>[0]> = {}) {
  return render(
    <MemoryRouter>
      <ReviewPublishStep tutor={tutor} hasAvailability={false} onPublish={vi.fn()} isPublishing={false} {...overrides} />
    </MemoryRouter>,
  );
}

describe("ReviewPublishStep", () => {
  it("disables Publish and explains why when Subject or Hourly Rate is missing", () => {
    renderStep(BASE_TUTOR);

    expect(screen.getByRole("button", { name: "Publish profile" })).toBeDisabled();
    expect(
      screen.getByText("A primary subject and an hourly rate are required before you can publish."),
    ).toBeInTheDocument();
  });

  it("enables Publish once Subject and Hourly Rate are both set", () => {
    renderStep({ ...BASE_TUTOR, subject: "Mathematics", hourlyRate: 500_000 });

    expect(screen.getByRole("button", { name: "Publish profile" })).toBeEnabled();
  });

  it("shows a submitted confirmation instead of the Publish button once already submitted", () => {
    renderStep({ ...BASE_TUTOR, subject: "Mathematics", hourlyRate: 500_000, profileStatus: "Submitted" });

    expect(screen.getByText("Your profile has been submitted and is awaiting Admin review.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Publish profile" })).not.toBeInTheDocument();
  });

  it("previews additional subjects with their level", () => {
    renderStep({
      ...BASE_TUTOR,
      tutorSubjects: [
        { subject: "Mathematics", level: "Beginner" },
        { subject: "Physics", level: null },
      ],
    });

    expect(screen.getByText("Mathematics (Beginner), Physics")).toBeInTheDocument();
  });

  it("previews trial lesson pricing only when available", () => {
    const { rerender } = renderStep(BASE_TUTOR);
    expect(screen.getByText("Not offered")).toBeInTheDocument();

    rerender(
      <MemoryRouter>
        <ReviewPublishStep
          tutor={{ ...BASE_TUTOR, trialLessonAvailable: true, trialLessonPrice: 100_000 }}
          hasAvailability={false}
          onPublish={vi.fn()}
          isPublishing={false}
        />
      </MemoryRouter>,
    );
    expect(screen.getByText("Available — 10,000 Toman")).toBeInTheDocument();
  });

  it("shows a publish error alert when publishing failed", () => {
    renderStep(
      { ...BASE_TUTOR, subject: "Mathematics", hourlyRate: 500_000 },
      { publishError: "Could not publish your profile. Please try again." },
    );

    expect(screen.getByText("Could not publish your profile. Please try again.")).toBeInTheDocument();
  });

  it("jumps back to a section's step when its Edit link is clicked", async () => {
    const onEditSection = vi.fn();
    renderStep(BASE_TUTOR, { onEditSection });

    const editButtons = screen.getAllByRole("button", { name: "Edit" });
    await userEvent.click(editButtons[1]);

    expect(onEditSection).toHaveBeenCalledWith(1);
  });

  it("does not render Edit links when onEditSection is omitted", () => {
    renderStep(BASE_TUTOR);

    expect(screen.queryByRole("button", { name: "Edit" })).not.toBeInTheDocument();
  });
});
