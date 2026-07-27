import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { MessagesPage } from "@/routes/MessagesPage";

describe("MessagesPage", () => {
  it("shows an honest coming-soon placeholder with a real path forward", () => {
    render(
      <MemoryRouter>
        <MessagesPage />
      </MemoryRouter>,
    );

    expect(screen.getByRole("heading", { name: "Messages" })).toBeInTheDocument();
    expect(screen.getByText("Messaging is coming soon")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Find Tutors" })).toHaveAttribute(
      "href",
      "/discovery/tutors/search",
    );
  });
});
