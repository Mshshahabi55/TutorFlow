import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { LoadingState } from "@/shared/components/feedback/LoadingState";

describe("LoadingState", () => {
  it("renders the default label with an accessible status role", () => {
    render(<LoadingState />);

    expect(screen.getByRole("status")).toHaveTextContent("Loading…");
  });

  it("renders a custom label", () => {
    render(<LoadingState label="Checking backend health…" />);

    expect(screen.getByRole("status")).toHaveTextContent("Checking backend health…");
  });
});
