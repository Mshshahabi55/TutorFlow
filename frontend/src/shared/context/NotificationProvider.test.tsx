import { describe, expect, it } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { NotificationProvider } from "@/shared/context/NotificationProvider";
import { useNotification } from "@/shared/hooks/useNotification";

function TestTrigger() {
  const { notify } = useNotification();
  return (
    <button onClick={() => notify({ message: "Tutor approved", severity: "success" })}>
      Notify
    </button>
  );
}

describe("NotificationProvider / useNotification", () => {
  it("shows a notification's message after notify() is called", async () => {
    render(
      <NotificationProvider>
        <TestTrigger />
      </NotificationProvider>,
    );

    await userEvent.click(screen.getByRole("button", { name: "Notify" }));

    expect(await screen.findByText("Tutor approved")).toBeInTheDocument();
  });

  it("shows a second queued notification after the first is dismissed", async () => {
    function DoubleTrigger() {
      const { notify } = useNotification();
      return (
        <button
          onClick={() => {
            notify({ message: "First" });
            notify({ message: "Second" });
          }}
        >
          Notify twice
        </button>
      );
    }

    render(
      <NotificationProvider>
        <DoubleTrigger />
      </NotificationProvider>,
    );

    await userEvent.click(screen.getByRole("button", { name: "Notify twice" }));

    expect(await screen.findByText("First")).toBeInTheDocument();

    const closeButton = screen.getByRole("button", { name: /close/i });
    await userEvent.click(closeButton);

    await waitFor(() => expect(screen.getByText("Second")).toBeInTheDocument());
  });
});
