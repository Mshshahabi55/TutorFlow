import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { UnitText } from "@/shared/components/UnitText";

describe("UnitText", () => {
  it("renders the value and the unit", () => {
    render(<UnitText value="17:30" unit="Tehran" />);

    expect(screen.getByText("17:30")).toBeInTheDocument();
    expect(screen.getByText("Tehran")).toBeInTheDocument();
  });

  it("renders a numeric value", () => {
    render(<UnitText value="50,000" unit="Toman" />);

    expect(screen.getByText("50,000")).toBeInTheDocument();
    expect(screen.getByText("Toman")).toBeInTheDocument();
  });
});
