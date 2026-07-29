import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { ApiRequestError } from "@/services/api/ApiRequestError";
import { ErrorType } from "@/services/api/apiTypes";

describe("ErrorState", () => {
  it("renders an ApiRequestError's own message", () => {
    const error = new ApiRequestError(
      { code: "BookSessionCommand.SlotAlreadyBooked", message: "This slot has already been booked.", type: ErrorType.Domain },
      409,
    );

    render(<ErrorState error={error} />);

    expect(screen.getByText("This slot has already been booked.")).toBeInTheDocument();
  });

  it("falls back to a generic message for a non-Error value", () => {
    render(<ErrorState error="not an error instance" />);

    expect(screen.getByText("We couldn't load this right now. Please try again.")).toBeInTheDocument();
  });

  it("invokes onRetry when the retry button is clicked", async () => {
    const onRetry = vi.fn();
    render(<ErrorState error={new Error("boom")} onRetry={onRetry} />);

    await userEvent.click(screen.getByRole("button", { name: /try again/i }));

    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("renders no retry button when onRetry is not supplied", () => {
    render(<ErrorState error={new Error("boom")} />);

    expect(screen.queryByRole("button", { name: /try again/i })).not.toBeInTheDocument();
  });
});
