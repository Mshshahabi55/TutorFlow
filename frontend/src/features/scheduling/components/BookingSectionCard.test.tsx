import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { BookingSectionCard } from "@/features/scheduling/components/BookingSectionCard";

describe("BookingSectionCard", () => {
  it("renders the title as a heading and the children", () => {
    render(
      <BookingSectionCard title="Availability">
        <p>content</p>
      </BookingSectionCard>,
    );

    expect(screen.getByRole("heading", { name: "Availability" })).toBeInTheDocument();
    expect(screen.getByText("content")).toBeInTheDocument();
  });

  it("labels the section region with its own heading for assistive tech", () => {
    render(
      <BookingSectionCard title="Booking Summary">
        <p>content</p>
      </BookingSectionCard>,
    );

    expect(screen.getByRole("region", { name: "Booking Summary" })).toBeInTheDocument();
  });
});
