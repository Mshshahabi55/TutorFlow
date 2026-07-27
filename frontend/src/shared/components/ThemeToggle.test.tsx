import { describe, expect, it, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ThemeToggle } from "@/shared/components/ThemeToggle";
import { ColorModeProvider } from "@/shared/context/ColorModeProvider";

function renderToggle() {
  return render(
    <ColorModeProvider>
      <ThemeToggle />
    </ColorModeProvider>,
  );
}

describe("ThemeToggle", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("shows a 'switch to dark mode' control while light", () => {
    renderToggle();

    expect(screen.getByRole("button", { name: "Switch to dark mode" })).toBeInTheDocument();
  });

  it("switches to dark, then back to light, on successive clicks", async () => {
    renderToggle();

    await userEvent.click(screen.getByRole("button", { name: "Switch to dark mode" }));
    expect(screen.getByRole("button", { name: "Switch to light mode" })).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Switch to light mode" }));
    expect(screen.getByRole("button", { name: "Switch to dark mode" })).toBeInTheDocument();
  });

  it("persists the choice across a fresh mount", async () => {
    const { unmount } = renderToggle();

    await userEvent.click(screen.getByRole("button", { name: "Switch to dark mode" }));
    unmount();

    renderToggle();

    expect(screen.getByRole("button", { name: "Switch to light mode" })).toBeInTheDocument();
  });
});
