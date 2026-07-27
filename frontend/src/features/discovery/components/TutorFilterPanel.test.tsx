import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitForElementToBeRemoved } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ThemeProvider } from "@mui/material/styles";
import { theme } from "@/app/theme";
import { TutorFilterPanel } from "@/features/discovery/components/TutorFilterPanel";

function mockViewport(matches: boolean) {
  window.matchMedia = vi.fn((query: string): MediaQueryList => ({
    matches,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }));
}

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
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders the fields inline on a desktop-sized viewport, with no Filters button", () => {
    mockViewport(true);
    renderPanel();

    expect(screen.getByLabelText("Language")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Filters/ })).not.toBeInTheDocument();
  });

  it("hides the fields behind a Filters button on a mobile-sized viewport, until opened", async () => {
    mockViewport(false);
    renderPanel();

    expect(screen.queryByLabelText("Language")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Filters" }));

    expect(await screen.findByLabelText("Language")).toBeInTheDocument();
  });

  it("shows the active filter count in the mobile Filters button label", () => {
    mockViewport(false);
    renderPanel(2);

    expect(screen.getByRole("button", { name: "Filters (2)" })).toBeInTheDocument();
  });

  it("closes the drawer from its own close button", async () => {
    mockViewport(false);
    renderPanel();

    await userEvent.click(screen.getByRole("button", { name: "Filters" }));
    await screen.findByLabelText("Language");

    await userEvent.click(screen.getByRole("button", { name: "Close filters" }));

    await waitForElementToBeRemoved(() => screen.queryByLabelText("Language"));
  });
});
