import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ActiveFiltersBar } from "@/features/discovery/components/ActiveFiltersBar";

describe("ActiveFiltersBar", () => {
  it("renders nothing when no filter is active", () => {
    const { container } = render(<ActiveFiltersBar activeFilters={[]} onClearAll={vi.fn()} />);

    expect(container).toBeEmptyDOMElement();
  });

  it("shows each active filter as a removable chip, and a Clear filters action", async () => {
    const onClearOne = vi.fn();
    const onClearAll = vi.fn();

    render(
      <ActiveFiltersBar
        activeFilters={[{ key: "subject", label: "Subject: Mathematics", onClear: onClearOne }]}
        onClearAll={onClearAll}
      />,
    );

    expect(screen.getByText("Subject: Mathematics")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Clear filters" }));
    expect(onClearAll).toHaveBeenCalledOnce();
  });
});
