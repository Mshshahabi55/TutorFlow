import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { LearningPlanSkeleton } from "@/features/learningPlans/components/LearningPlanSkeleton";

describe("LearningPlanSkeleton", () => {
  it("renders a loading placeholder, matching LearningPlanCard's own test id convention", () => {
    render(<LearningPlanSkeleton />);
    expect(screen.getByTestId("learning-plan-card-skeleton")).toBeInTheDocument();
  });
});
