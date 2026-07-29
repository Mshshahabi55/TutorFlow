import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MonthCalendarGrid } from "@/features/scheduling/components/MonthCalendarGrid";

describe("MonthCalendarGrid", () => {
  it("renders a month header and delegates every cell to renderDay", () => {
    render(
      <MonthCalendarGrid
        monthKey="2026-08"
        onNavigateMonth={vi.fn()}
        renderDay={(dateKey) => <span data-testid="cell">{dateKey}</span>}
      />,
    );

    expect(screen.getByText("August 2026")).toBeInTheDocument();
    // 31 days in August, plus leading/trailing padding from adjacent months.
    expect(screen.getAllByTestId("cell").length).toBeGreaterThanOrEqual(31);
  });

  it("passes isCurrentMonth/isPast/isToday meta through to renderDay", () => {
    render(
      <MonthCalendarGrid
        monthKey="2026-08"
        onNavigateMonth={vi.fn()}
        renderDay={(dateKey, meta) => (
          <span data-testid={`cell-${dateKey}`}>{meta.isCurrentMonth ? "in" : "out"}</span>
        )}
      />,
    );

    expect(screen.getByTestId("cell-2026-08-15")).toHaveTextContent("in");
    expect(screen.getByTestId("cell-2026-07-26")).toHaveTextContent("out");
  });

  it("calls onNavigateMonth with -1/1 when the arrow buttons are clicked", async () => {
    const onNavigateMonth = vi.fn();
    render(<MonthCalendarGrid monthKey="2026-08" onNavigateMonth={onNavigateMonth} renderDay={() => null} />);

    await userEvent.click(screen.getByRole("button", { name: "Previous month" }));
    await userEvent.click(screen.getByRole("button", { name: "Next month" }));

    expect(onNavigateMonth).toHaveBeenNthCalledWith(1, -1);
    expect(onNavigateMonth).toHaveBeenNthCalledWith(2, 1);
  });
});
