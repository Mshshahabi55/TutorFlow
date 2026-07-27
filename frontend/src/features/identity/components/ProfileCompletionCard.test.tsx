import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { ProfileCompletionCard } from "@/features/identity/components/ProfileCompletionCard";
import { deriveProfileCompletion } from "@/features/identity/utils/profileCompletion";
import type { TutorDto } from "@/services/api/dtos";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";

const BASE_TUTOR: TutorDto = {
  tutorId: TUTOR_ID,
  isApproved: false,
  isSuspended: false,
  isDiscoverable: true,
  hourlyRate: null,
  subject: null,
  language: null,
  location: null,
  offeredDurations: [],
};

function renderCard(tutor: TutorDto, hasAvailability: boolean) {
  return render(
    <MemoryRouter>
      <ProfileCompletionCard
        completion={deriveProfileCompletion(tutor, hasAvailability)}
        tutorId={TUTOR_ID}
      />
    </MemoryRouter>,
  );
}

describe("ProfileCompletionCard", () => {
  it("shows the step count and a Complete your profile CTA when incomplete", () => {
    renderCard(BASE_TUTOR, false);

    expect(screen.getByText("0 of 5 steps complete.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Complete your profile" })).toHaveAttribute(
      "href",
      `/identity/tutors/${TUTOR_ID}/edit`,
    );
  });

  it("shows a completion message and no CTA once every item is done", () => {
    renderCard(
      {
        ...BASE_TUTOR,
        isApproved: true,
        hourlyRate: 500_000,
        subject: "Mathematics",
        language: "English",
        offeredDurations: ["01:00:00"],
      },
      true,
    );

    expect(screen.getByText("Your profile is complete.")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Complete your profile" })).not.toBeInTheDocument();
  });

  it("lists every checklist item by label", () => {
    renderCard(BASE_TUTOR, false);

    expect(screen.getByText("Profile completed")).toBeInTheDocument();
    expect(screen.getByText("Availability added")).toBeInTheDocument();
    expect(screen.getByText("Teaching subjects added")).toBeInTheDocument();
    expect(screen.getByText("Languages added")).toBeInTheDocument();
    expect(screen.getByText("Verification completed")).toBeInTheDocument();
  });
});
