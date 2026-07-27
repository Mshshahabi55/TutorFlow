import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { ChildSummaryCard } from "@/features/identity/components/ChildSummaryCard";
import { RelationshipStatus } from "@/services/api/dtos";
import type { RelationshipDto } from "@/services/api/dtos";

const RELATIONSHIP: RelationshipDto = {
  relationshipId: "r1",
  parentGuardianId: "pg1",
  studentId: "st1",
  status: RelationshipStatus.Confirmed,
};

function renderCard(relationship: RelationshipDto) {
  return render(
    <MemoryRouter>
      <ChildSummaryCard relationship={relationship} />
    </MemoryRouter>,
  );
}

describe("ChildSummaryCard", () => {
  it("shows the Student id and a Confirmed badge, with a View sessions link, for a Confirmed relationship", () => {
    renderCard(RELATIONSHIP);

    expect(screen.getByText("st1")).toBeInTheDocument();
    expect(screen.getByText("Confirmed")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /View sessions/ })).toHaveAttribute(
      "href",
      "/scheduling/students/st1/schedule",
    );
  });

  it("shows an Invited badge and no View sessions link for an unconfirmed relationship", () => {
    renderCard({ ...RELATIONSHIP, status: RelationshipStatus.Invited });

    expect(screen.getByText("Invited")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /View sessions/ })).not.toBeInTheDocument();
    expect(screen.getByText(/waiting for this relationship to be confirmed/i)).toBeInTheDocument();
  });
});
