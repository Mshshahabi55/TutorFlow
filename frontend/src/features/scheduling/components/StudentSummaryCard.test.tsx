import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { StudentSummaryCard } from "@/features/scheduling/components/StudentSummaryCard";

describe("StudentSummaryCard", () => {
  it("shows the Student id, since StudentDto has no name field", () => {
    render(<StudentSummaryCard student={{ studentId: "st1", isMinor: false }} />);

    expect(screen.getByText("st1")).toBeInTheDocument();
    expect(screen.queryByText("Minor")).not.toBeInTheDocument();
  });

  it("shows a Minor badge only when the Student is a minor", () => {
    render(<StudentSummaryCard student={{ studentId: "st1", isMinor: true }} />);

    expect(screen.getByText("Minor")).toBeInTheDocument();
  });
});
