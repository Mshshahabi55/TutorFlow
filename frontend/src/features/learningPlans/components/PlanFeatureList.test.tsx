import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { PlanFeatureList } from "@/features/learningPlans/components/PlanFeatureList";

describe("PlanFeatureList", () => {
  it("renders every feature as its own list item", () => {
    render(<PlanFeatureList features={["2 lessons / week", "8 total lessons"]} />);

    expect(screen.getByRole("list")).toBeInTheDocument();
    const items = screen.getAllByRole("listitem");
    expect(items).toHaveLength(2);
    expect(screen.getByText("2 lessons / week")).toBeInTheDocument();
    expect(screen.getByText("8 total lessons")).toBeInTheDocument();
  });

  it("renders nothing but an empty list when given no features", () => {
    render(<PlanFeatureList features={[]} />);

    expect(screen.queryAllByRole("listitem")).toHaveLength(0);
  });
});
