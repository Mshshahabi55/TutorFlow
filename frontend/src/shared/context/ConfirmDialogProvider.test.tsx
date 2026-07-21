import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ConfirmDialogProvider } from "@/shared/context/ConfirmDialogProvider";
import { useConfirmDialog } from "@/shared/hooks/useConfirmDialog";
import { useState } from "react";

function TestTrigger() {
  const { confirm } = useConfirmDialog();
  const [result, setResult] = useState<string>("pending");

  return (
    <>
      <button
        onClick={() => {
          void confirm({
            title: "Suspend this Tutor?",
            description: "They will no longer be discoverable.",
            confirmLabel: "Suspend",
            destructive: true,
          }).then((confirmed) => setResult(confirmed ? "confirmed" : "cancelled"));
        }}
      >
        Open confirm
      </button>
      <div data-testid="result">{result}</div>
    </>
  );
}

describe("ConfirmDialogProvider / useConfirmDialog", () => {
  it("resolves true when the confirm action is clicked", async () => {
    render(
      <ConfirmDialogProvider>
        <TestTrigger />
      </ConfirmDialogProvider>,
    );

    await userEvent.click(screen.getByRole("button", { name: "Open confirm" }));
    expect(screen.getByText("Suspend this Tutor?")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Suspend" }));

    expect(await screen.findByTestId("result")).toHaveTextContent("confirmed");
  });

  it("resolves false when Cancel is clicked", async () => {
    render(
      <ConfirmDialogProvider>
        <TestTrigger />
      </ConfirmDialogProvider>,
    );

    await userEvent.click(screen.getByRole("button", { name: "Open confirm" }));
    await userEvent.click(screen.getByRole("button", { name: "Cancel" }));

    expect(await screen.findByTestId("result")).toHaveTextContent("cancelled");
  });
});
