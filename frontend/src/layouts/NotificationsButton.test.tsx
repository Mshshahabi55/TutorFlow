import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { NotificationsButton } from "@/layouts/NotificationsButton";

describe("NotificationsButton", () => {
  it("shows no badge or count — there is nothing real to count", () => {
    render(<NotificationsButton />);

    expect(screen.queryByText(/^\d+$/)).not.toBeInTheDocument();
  });

  it("opens an honest 'no notifications' message on click", async () => {
    render(<NotificationsButton />);

    await userEvent.click(screen.getByRole("button", { name: "Notifications" }));

    expect(screen.getByText("No notifications yet.")).toBeInTheDocument();
  });
});
