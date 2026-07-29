import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { LearningPlanCard } from "@/features/learningPlans/components/LearningPlanCard";
import type { LearningPlanPreview } from "@/features/learningPlans/types";

const PLAN: LearningPlanPreview = {
  learningPlanId: "plan-1",
  title: "IELTS Intensive",
  durationDays: 45,
  sessionsPerWeek: 4,
  totalSessions: 18,
  price: 12_000_000,
  description: "Focused preparation for the IELTS exam.",
  status: "Active",
};

describe("LearningPlanCard", () => {
  it("shows the plan's title, duration, description, features, price, and status", () => {
    render(<LearningPlanCard plan={PLAN} />);

    expect(screen.getByRole("heading", { name: "IELTS Intensive" })).toBeInTheDocument();
    expect(screen.getByText("45 Days")).toBeInTheDocument();
    expect(screen.getByText("Focused preparation for the IELTS exam.")).toBeInTheDocument();
    expect(screen.getByText("4 lessons / week")).toBeInTheDocument();
    expect(screen.getByText("18 total lessons")).toBeInTheDocument();
    expect(screen.getByText("1,200,000 Toman")).toBeInTheDocument();
    expect(screen.getByText("Active")).toBeInTheDocument();
  });

  it("disables Enroll when no handler is given, rather than wiring it to a fake action", () => {
    render(<LearningPlanCard plan={PLAN} />);

    expect(screen.getByRole("button", { name: "Enroll" })).toBeDisabled();
  });

  it("enables Enroll and invokes the handler with this plan when one is given", async () => {
    const onEnroll = vi.fn();
    render(<LearningPlanCard plan={PLAN} onEnroll={onEnroll} />);

    const enrollButton = screen.getByRole("button", { name: "Enroll" });
    expect(enrollButton).toBeEnabled();

    await userEvent.click(enrollButton);
    expect(onEnroll).toHaveBeenCalledWith(PLAN);
  });

  it("uses singular wording for a plan with exactly one lesson per week and one total lesson", () => {
    render(
      <LearningPlanCard
        plan={{ ...PLAN, sessionsPerWeek: 1, totalSessions: 1 }}
      />,
    );

    expect(screen.getByText("1 lesson / week")).toBeInTheDocument();
    expect(screen.getByText("1 total lesson")).toBeInTheDocument();
  });
});
