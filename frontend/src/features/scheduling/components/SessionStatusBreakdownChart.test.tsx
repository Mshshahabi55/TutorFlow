import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { SessionStatusBreakdownChart } from "@/features/scheduling/components/SessionStatusBreakdownChart";

describe("SessionStatusBreakdownChart", () => {
  it("renders every status bucket's label and count", () => {
    render(<SessionStatusBreakdownChart counts={{ scheduled: 2, completed: 1, cancelled: 1, noShow: 1 }} />);

    expect(screen.getByText("Upcoming")).toBeInTheDocument();
    expect(screen.getByText("2")).toBeInTheDocument();
    expect(screen.getByText("Completed")).toBeInTheDocument();
    expect(screen.getByText("Cancelled")).toBeInTheDocument();
    expect(screen.getByText("No-Show")).toBeInTheDocument();
  });

  it("shows every status bucket at zero when there are no sessions, rather than omitting them", () => {
    render(<SessionStatusBreakdownChart counts={{ scheduled: 0, completed: 0, cancelled: 0, noShow: 0 }} />);

    expect(screen.getAllByText("0")).toHaveLength(4);
  });
});
