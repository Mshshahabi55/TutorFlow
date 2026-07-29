import { beforeEach, describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { FavoriteToggleButton } from "@/features/discovery/components/FavoriteToggleButton";

const TUTOR_ID = "11111111-1111-1111-1111-111111111111";

describe("FavoriteToggleButton", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("starts unfavorited", () => {
    render(<FavoriteToggleButton tutorId={TUTOR_ID} />);

    expect(screen.getByRole("button", { name: "Add to favorites" })).toHaveAttribute("aria-pressed", "false");
  });

  it("toggles to favorited on click and persists it", async () => {
    const user = userEvent.setup();
    render(<FavoriteToggleButton tutorId={TUTOR_ID} />);

    await user.click(screen.getByRole("button", { name: "Add to favorites" }));

    expect(screen.getByRole("button", { name: "Remove from favorites" })).toHaveAttribute("aria-pressed", "true");
    expect(JSON.parse(window.localStorage.getItem("tutorflow.favoriteTutorIds") ?? "[]")).toEqual([TUTOR_ID]);
  });
});
