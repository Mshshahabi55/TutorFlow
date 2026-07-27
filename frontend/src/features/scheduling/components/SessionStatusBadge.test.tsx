import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { SessionStatusBadge } from "@/features/scheduling/components/SessionStatusBadge";
import { SessionStatus } from "@/services/api/dtos";

describe("SessionStatusBadge", () => {
  it.each([
    [SessionStatus.Scheduled, "Scheduled"],
    [SessionStatus.Completed, "Completed"],
    [SessionStatus.Cancelled, "Cancelled"],
    [SessionStatus.NoShow, "No-Show"],
  ])("shows the label for %s as %s", (status, label) => {
    render(<SessionStatusBadge status={status} />);

    expect(screen.getByText(label)).toBeInTheDocument();
  });
});
