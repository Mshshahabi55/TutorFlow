import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { OnboardingWelcomeScreen } from "@/features/identity/components/onboarding/OnboardingWelcomeScreen";

const STEPS = ["Personal Information", "Teaching Information", "Review & Publish"] as const;

describe("OnboardingWelcomeScreen", () => {
  it("lists every step and the time estimate", () => {
    render(<OnboardingWelcomeScreen steps={STEPS} onStart={vi.fn()} />);

    for (const label of STEPS) {
      expect(screen.getByText(label)).toBeInTheDocument();
    }
    expect(screen.getByText("About 8–10 minutes")).toBeInTheDocument();
  });

  it("calls onStart when the CTA is clicked", async () => {
    const onStart = vi.fn();
    render(<OnboardingWelcomeScreen steps={STEPS} onStart={onStart} />);

    await userEvent.click(screen.getByRole("button", { name: "Get started" }));

    expect(onStart).toHaveBeenCalledTimes(1);
  });
});
