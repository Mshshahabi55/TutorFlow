import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { StatusPill } from "@/shared/components/feedback/StatusPill";

describe("StatusPill", () => {
  it("renders the given label", () => {
    render(<StatusPill label="Scheduled" tone="info" />);

    expect(screen.getByText("Scheduled")).toBeInTheDocument();
  });

  it("defaults to a neutral tone", () => {
    render(<StatusPill label="Unknown" />);

    expect(screen.getByText("Unknown")).toBeInTheDocument();
  });
});
