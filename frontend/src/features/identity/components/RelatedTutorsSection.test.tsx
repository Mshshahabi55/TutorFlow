import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "@/test/renderWithProviders";
import { RelatedTutorsSection } from "@/features/identity/components/RelatedTutorsSection";
import * as discoveryService from "@/features/discovery/api/discoveryService";
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

const OTHER_TUTOR: TutorDto = {
  ...TUTOR,
  tutorId: "22222222-2222-2222-2222-222222222222",
  hourlyRate: 300_000,
};

describe("RelatedTutorsSection", () => {
  it("renders nothing when the Tutor has no subject set", () => {
    vi.spyOn(discoveryService, "searchTutors");
    const { container } = renderWithProviders(
      <RelatedTutorsSection tutor={{ ...TUTOR, subject: null }} />,
    );

    expect(container).toBeEmptyDOMElement();
  });

  it("searches by this Tutor's own subject, using the existing search capability", async () => {
    const searchTutors = vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [OTHER_TUTOR],
      totalCount: 1,
      page: 1,
      pageSize: 4,
    });

    renderWithProviders(<RelatedTutorsSection tutor={TUTOR} />);

    expect(await screen.findByRole("heading", { name: "More tutors teaching Mathematics" })).toBeInTheDocument();
    expect(searchTutors).toHaveBeenCalledWith(
      { subject: "Mathematics", language: "", location: "", availableFrom: "" },
      1,
      4,
    );
  });

  it("excludes the Tutor being viewed from their own related list", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [TUTOR, OTHER_TUTOR],
      totalCount: 2,
      page: 1,
      pageSize: 4,
    });

    renderWithProviders(<RelatedTutorsSection tutor={TUTOR} />);

    // Wait for the loaded card itself (the heading also renders during the
    // loading/skeleton state, so it isn't proof the query has resolved).
    expect(await screen.findByText("30,000 Toman/hr")).toBeInTheDocument();
    expect(screen.getAllByText(/Toman\/hr/)).toHaveLength(1);
  });

  it("renders nothing when no other Tutor teaches the same subject", async () => {
    vi.spyOn(discoveryService, "searchTutors").mockResolvedValue({
      items: [TUTOR],
      totalCount: 1,
      page: 1,
      pageSize: 4,
    });

    const { container } = renderWithProviders(<RelatedTutorsSection tutor={TUTOR} />);

    await vi.waitFor(() =>
      expect(screen.queryByTestId("tutor-card-skeleton")).not.toBeInTheDocument(),
    );
    expect(container).toBeEmptyDOMElement();
  });
});
