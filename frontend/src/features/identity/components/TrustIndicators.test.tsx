import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { TrustIndicators } from "@/features/identity/components/TrustIndicators";
import type { TutorDto } from "@/services/api/dtos";

const BASE_TUTOR: TutorDto = {
  tutorId: "11111111-1111-1111-1111-111111111111",
  isApproved: false,
  isSuspended: false,
  isDiscoverable: true,
  hourlyRate: 500_000,
  subject: "Mathematics",
  language: null,
  location: null,
  offeredDurations: [],
};

describe("TrustIndicators", () => {
  it("renders nothing when no real trust fact applies", () => {
    const { container } = render(<TrustIndicators tutor={BASE_TUTOR} />);

    expect(container).toBeEmptyDOMElement();
  });

  it("shows Verified only when the Tutor is approved, never invented", () => {
    render(<TrustIndicators tutor={{ ...BASE_TUTOR, isApproved: true }} />);

    expect(screen.getByText("Verified")).toBeInTheDocument();
  });

  it("shows the Tutor's real spoken language", () => {
    render(<TrustIndicators tutor={{ ...BASE_TUTOR, language: "English" }} />);

    expect(screen.getByText("Speaks English")).toBeInTheDocument();
  });

  it("omits the language fact when the caller already shows it as its own badge", () => {
    render(<TrustIndicators tutor={{ ...BASE_TUTOR, language: "English" }} showLanguage={false} />);

    expect(screen.queryByText(/Speaks English/)).not.toBeInTheDocument();
  });

  it("shows a next-available fact only when the caller supplies one from real availability data", () => {
    const { rerender } = render(<TrustIndicators tutor={BASE_TUTOR} nextAvailableLabel="Sat, Aug 01" />);
    expect(screen.getByText("Next available Sat, Aug 01")).toBeInTheDocument();

    rerender(<TrustIndicators tutor={BASE_TUTOR} nextAvailableLabel={null} />);
    expect(screen.queryByText(/Next available/)).not.toBeInTheDocument();
  });
});
