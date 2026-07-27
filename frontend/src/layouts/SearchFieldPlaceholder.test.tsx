import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SearchFieldPlaceholder } from "@/layouts/SearchFieldPlaceholder";
import { apiClient } from "@/services/api/apiClient";

describe("SearchFieldPlaceholder", () => {
  it("lets the user type into it", async () => {
    render(<SearchFieldPlaceholder />);

    const field = screen.getByLabelText("Search");
    await userEvent.type(field, "algebra tutor");

    expect(field).toHaveValue("algebra tutor");
  });

  it("never makes a backend request, however it's used", async () => {
    const getSpy = vi.spyOn(apiClient, "get");
    const postSpy = vi.spyOn(apiClient, "post");

    render(<SearchFieldPlaceholder />);
    await userEvent.type(screen.getByLabelText("Search"), "algebra tutor{Enter}");

    expect(getSpy).not.toHaveBeenCalled();
    expect(postSpy).not.toHaveBeenCalled();
  });
});
