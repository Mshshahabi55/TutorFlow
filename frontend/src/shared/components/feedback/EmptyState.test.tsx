import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { EmptyState } from "@/shared/components/feedback/EmptyState";

describe("EmptyState", () => {
  it("renders the title and optional description", () => {
    render(<EmptyState title="No results" description="Try a different filter." />);

    expect(screen.getByText("No results")).toBeInTheDocument();
    expect(screen.getByText("Try a different filter.")).toBeInTheDocument();
  });

  it("renders without a description", () => {
    render(<EmptyState title="No results" />);

    expect(screen.getByText("No results")).toBeInTheDocument();
  });
});
