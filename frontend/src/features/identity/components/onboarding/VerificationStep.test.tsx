import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { VerificationStep } from "@/features/identity/components/onboarding/VerificationStep";
import type { TutorDto } from "@/services/api/dtos";

const BASE_TUTOR: TutorDto = {
  tutorId: "11111111-1111-1111-1111-111111111111",
  isApproved: false,
  isSuspended: false,
  isDiscoverable: false,
  hourlyRate: null,
  subject: null,
  language: null,
  location: null,
  offeredDurations: [],
  profileStatus: "Draft",
};

describe("VerificationStep", () => {
  it("shows the self-attestation explanation, never a fabricated document-verification status", () => {
    render(<VerificationStep tutor={BASE_TUTOR} />);

    expect(
      screen.getByText(/TutorFlow doesn.t verify documents or run background checks in v1/),
    ).toBeInTheDocument();
  });

  it("reflects a Draft, not-yet-approved profile honestly", () => {
    render(<VerificationStep tutor={BASE_TUTOR} />);

    expect(screen.getByText("Profile in progress")).toBeInTheDocument();
    expect(screen.getByText("Pending Admin review")).toBeInTheDocument();
  });

  it("reflects a Submitted, approved profile honestly", () => {
    render(<VerificationStep tutor={{ ...BASE_TUTOR, profileStatus: "Submitted", isApproved: true }} />);

    expect(screen.getByText("Profile submitted")).toBeInTheDocument();
    expect(screen.getByText("Approved")).toBeInTheDocument();
  });
});
