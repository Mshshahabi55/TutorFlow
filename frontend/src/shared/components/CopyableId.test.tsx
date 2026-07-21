import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CopyableId } from "@/shared/components/CopyableId";

describe("CopyableId", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders the id and copies it to the clipboard on click", async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.assign(navigator, { clipboard: { writeText } });

    render(<CopyableId id="11111111-1111-1111-1111-111111111111" />);

    expect(screen.getByText("11111111-1111-1111-1111-111111111111")).toBeInTheDocument();

    await userEvent.click(screen.getByLabelText("Copy id"));

    expect(writeText).toHaveBeenCalledWith("11111111-1111-1111-1111-111111111111");
  });
});
