import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { CompareBar } from "@/features/discovery/components/CompareBar";
import type { TutorDto } from "@/services/api/dtos";

function tutor(overrides: Partial<TutorDto>): TutorDto {
  return {
    tutorId: "11111111-1111-1111-1111-111111111111",
    isApproved: true,
    isSuspended: false,
    isDiscoverable: true,
    hourlyRate: 500_000,
    subject: "Mathematics",
    language: "English",
    location: "Remote",
    offeredDurations: [],
    ...overrides,
  };
}

describe("CompareBar", () => {
  it("renders nothing when no Tutor is selected", () => {
    const { container } = render(
      <MemoryRouter>
        <CompareBar selectedTutors={[]} onRemove={vi.fn()} onClear={vi.fn()} />
      </MemoryRouter>,
    );

    expect(container).toBeEmptyDOMElement();
  });

  it("shows a chip per selected Tutor and disables Compare now below the minimum", () => {
    render(
      <MemoryRouter>
        <CompareBar
          selectedTutors={[tutor({ tutorId: "a", displayName: "Ada" })]}
          onRemove={vi.fn()}
          onClear={vi.fn()}
        />
      </MemoryRouter>,
    );

    expect(screen.getByText("Ada")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Compare now" })).toHaveAttribute("aria-disabled", "true");
  });

  it("enables Compare now once at least two Tutors are selected, linking to their ids", () => {
    render(
      <MemoryRouter>
        <CompareBar
          selectedTutors={[tutor({ tutorId: "a", displayName: "Ada" }), tutor({ tutorId: "b", displayName: "Bo" })]}
          onRemove={vi.fn()}
          onClear={vi.fn()}
        />
      </MemoryRouter>,
    );

    const link = screen.getByRole("link", { name: "Compare now" });
    expect(link).not.toHaveAttribute("aria-disabled");
    expect(link).toHaveAttribute("href", "/discovery/tutors/compare?ids=a,b");
  });

  it("calls onRemove when a chip's delete icon is clicked", async () => {
    const user = userEvent.setup();
    const onRemove = vi.fn();
    render(
      <MemoryRouter>
        <CompareBar
          selectedTutors={[tutor({ tutorId: "a", displayName: "Ada" })]}
          onRemove={onRemove}
          onClear={vi.fn()}
        />
      </MemoryRouter>,
    );

    await user.click(screen.getByTestId("CancelIcon"));

    expect(onRemove).toHaveBeenCalledWith("a");
  });

  it("calls onClear when Clear is clicked", async () => {
    const user = userEvent.setup();
    const onClear = vi.fn();
    render(
      <MemoryRouter>
        <CompareBar selectedTutors={[tutor({ tutorId: "a" })]} onRemove={vi.fn()} onClear={onClear} />
      </MemoryRouter>,
    );

    await user.click(screen.getByRole("button", { name: "Clear" }));

    expect(onClear).toHaveBeenCalled();
  });
});
