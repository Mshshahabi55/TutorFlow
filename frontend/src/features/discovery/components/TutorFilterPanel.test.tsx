import { describe, expect, it } from "vitest";
import { render, screen, waitForElementToBeRemoved } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ThemeProvider } from "@mui/material/styles";
import { theme } from "@/app/theme";
import { TutorFilterPanel } from "@/features/discovery/components/TutorFilterPanel";

function renderPanel(activeFilterCount = 0) {
  return render(
    <ThemeProvider theme={theme}>
      <TutorFilterPanel activeFilterCount={activeFilterCount}>
        <label htmlFor="test-field">Language</label>
        <input id="test-field" />
      </TutorFilterPanel>
    </ThemeProvider>,
  );
}

describe("TutorFilterPanel", () => {
  it("hides the fields behind a Filters button until opened, regardless of viewport", async () => {
    renderPanel();

    expect(screen.queryByLabelText("Language")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Filters" }));

    expect(await screen.findByLabelText("Language")).toBeInTheDocument();
  });

  it("shows the active filter count in the Filters button label", () => {
    renderPanel(2);

    expect(screen.getByRole("button", { name: "Filters (2)" })).toBeInTheDocument();
  });

  it("closes the drawer from its own close button", async () => {
    renderPanel();

    await userEvent.click(screen.getByRole("button", { name: "Filters" }));
    await screen.findByLabelText("Language");

    await userEvent.click(screen.getByRole("button", { name: "Close filters" }));

    await waitForElementToBeRemoved(() => screen.queryByLabelText("Language"));
  });
});
