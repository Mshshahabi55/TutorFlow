import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { StudentRosterCard } from "@/features/scheduling/components/StudentRosterCard";

describe("StudentRosterCard", () => {
  it("shows the Student id and total lesson count", () => {
    render(
      <StudentRosterCard
        entry={{ studentId: "st-1", upcomingSessions: 0, totalSessions: 3 }}
      />,
    );

    expect(screen.getByText("st-1")).toBeInTheDocument();
    expect(screen.getByText("3 lessons together")).toBeInTheDocument();
  });

  it("uses singular 'lesson' for exactly one total session", () => {
    render(
      <StudentRosterCard
        entry={{ studentId: "st-1", upcomingSessions: 0, totalSessions: 1 }}
      />,
    );

    expect(screen.getByText("1 lesson together")).toBeInTheDocument();
  });

  it("shows an upcoming-count pill only when there is at least one upcoming session", () => {
    const { rerender } = render(
      <StudentRosterCard
        entry={{ studentId: "st-1", upcomingSessions: 2, totalSessions: 2 }}
      />,
    );

    expect(screen.getByText("2 upcoming")).toBeInTheDocument();

    rerender(
      <StudentRosterCard
        entry={{ studentId: "st-1", upcomingSessions: 0, totalSessions: 2 }}
      />,
    );

    expect(screen.queryByText(/upcoming/)).not.toBeInTheDocument();
  });
});
