import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { PlanBadge } from "@/features/learningPlans/components/PlanBadge";

describe("PlanBadge", () => {
  it("renders the given label", () => {
    render(<PlanBadge label="30 Days" tone="info" />);
    expect(screen.getByText("30 Days")).toBeInTheDocument();
  });

  it.each([
    ["Active", "MuiChip-colorSuccess"],
    ["Draft", "MuiChip-colorDefault"],
    ["Archived", "MuiChip-colorWarning"],
  ])("derives a tone automatically for the known status %s", (label, expectedClass) => {
    render(<PlanBadge label={label} />);
    expect(screen.getByText(label).closest(".MuiChip-root")).toHaveClass(expectedClass);
  });

  it("respects an explicit tone override rather than guessing from the label", () => {
    render(<PlanBadge label="45 Days" tone="info" />);
    expect(screen.getByText("45 Days").closest(".MuiChip-root")).toHaveClass("MuiChip-colorInfo");
  });
});
