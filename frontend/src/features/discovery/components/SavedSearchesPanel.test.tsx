import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SavedSearchesPanel } from "@/features/discovery/components/SavedSearchesPanel";
import type { SearchTutorsFilters } from "@/features/discovery/api/discoveryService";

const FILTERS: SearchTutorsFilters = { subject: "Math", language: "", location: "", availableFrom: "" };
const EMPTY_FILTERS: SearchTutorsFilters = { subject: "", language: "", location: "", availableFrom: "" };

describe("SavedSearchesPanel", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("disables Save this search when there are no active filters", () => {
    render(<SavedSearchesPanel currentFilters={EMPTY_FILTERS} hasActiveFilters={false} onApply={vi.fn()} />);

    expect(screen.getByRole("button", { name: "Save this search" })).toBeDisabled();
  });

  it("saves a named search and shows it as a chip", async () => {
    const user = userEvent.setup();
    render(<SavedSearchesPanel currentFilters={FILTERS} hasActiveFilters onApply={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Save this search" }));
    await user.type(screen.getByPlaceholderText("Name this search"), "Math tutors");
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(screen.getByText("Math tutors")).toBeInTheDocument();
  });

  it("calls onApply with the saved filters when its chip is clicked", async () => {
    const user = userEvent.setup();
    const onApply = vi.fn();
    render(<SavedSearchesPanel currentFilters={FILTERS} hasActiveFilters onApply={onApply} />);

    await user.click(screen.getByRole("button", { name: "Save this search" }));
    await user.type(screen.getByPlaceholderText("Name this search"), "Math tutors");
    await user.click(screen.getByRole("button", { name: "Save" }));
    await user.click(screen.getByText("Math tutors"));

    expect(onApply).toHaveBeenCalledWith(FILTERS);
  });

  it("removes a saved search when its delete icon is clicked", async () => {
    const user = userEvent.setup();
    render(<SavedSearchesPanel currentFilters={FILTERS} hasActiveFilters onApply={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Save this search" }));
    await user.type(screen.getByPlaceholderText("Name this search"), "Math tutors");
    await user.click(screen.getByRole("button", { name: "Save" }));
    await user.click(screen.getByTestId("DeleteOutlineRoundedIcon"));

    expect(screen.queryByText("Math tutors")).not.toBeInTheDocument();
  });
});
