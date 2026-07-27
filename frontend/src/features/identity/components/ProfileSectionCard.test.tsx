import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { ProfileSectionCard } from "@/features/identity/components/ProfileSectionCard";

describe("ProfileSectionCard", () => {
  it("renders the title as a heading and the children", () => {
    render(
      <ProfileSectionCard title="Reviews">
        <p>No reviews yet</p>
      </ProfileSectionCard>,
    );

    expect(screen.getByRole("heading", { name: "Reviews" })).toBeInTheDocument();
    expect(screen.getByText("No reviews yet")).toBeInTheDocument();
  });

  it("labels the section region with its own heading for assistive tech", () => {
    render(
      <ProfileSectionCard title="Teaching Information">
        <p>content</p>
      </ProfileSectionCard>,
    );

    expect(screen.getByRole("region", { name: "Teaching Information" })).toBeInTheDocument();
  });

  it("renders an optional action alongside the title", () => {
    render(
      <ProfileSectionCard title="Manage this listing" action={<button>Approve</button>}>
        <p>content</p>
      </ProfileSectionCard>,
    );

    expect(screen.getByRole("button", { name: "Approve" })).toBeInTheDocument();
  });
});
