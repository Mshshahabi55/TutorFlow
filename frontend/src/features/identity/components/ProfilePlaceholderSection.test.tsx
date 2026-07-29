import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import ReviewsRoundedIcon from "@mui/icons-material/ReviewsRounded";
import { ProfilePlaceholderSection } from "@/features/identity/components/ProfilePlaceholderSection";

describe("ProfilePlaceholderSection", () => {
  it("renders the section title, honest heading, and description", () => {
    render(
      <ProfilePlaceholderSection
        id="reviews"
        title="Reviews"
        icon={<ReviewsRoundedIcon aria-hidden="true" />}
        heading="No reviews yet"
        description="This tutor hasn't received any reviews yet."
      />,
    );

    expect(screen.getByRole("heading", { name: "Reviews" })).toBeInTheDocument();
    expect(screen.getByText("No reviews yet")).toBeInTheDocument();
    expect(screen.getByText("This tutor hasn't received any reviews yet.")).toBeInTheDocument();
  });
});
