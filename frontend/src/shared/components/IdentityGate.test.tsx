import { beforeEach, describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { IdentityGate } from "@/shared/components/IdentityGate";

describe("IdentityGate", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("shows the friendly setup prompt, never a bare id form, when nothing is remembered yet", () => {
    render(
      <IdentityGate kind="student" fieldLabel="Student id" title="Let's find your lessons" description="One-time setup.">
        {() => <p>content</p>}
      </IdentityGate>,
    );

    expect(screen.getByRole("heading", { name: "Let's find your lessons" })).toBeInTheDocument();
    expect(screen.getByText("One-time setup.")).toBeInTheDocument();
    expect(screen.getByLabelText("Student id")).toBeInTheDocument();
    expect(screen.queryByText("content")).not.toBeInTheDocument();
  });

  it("remembers the id after one entry and renders the gated content immediately on the next mount", async () => {
    const { unmount } = render(
      <IdentityGate kind="tutor" fieldLabel="Tutor id" title="Set up" description="desc">
        {(id) => <p>Gated content for {id}</p>}
      </IdentityGate>,
    );

    await userEvent.type(screen.getByLabelText("Tutor id"), "11111111-1111-1111-1111-111111111111");
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByText("Gated content for 11111111-1111-1111-1111-111111111111")).toBeInTheDocument();

    unmount();
    render(
      <IdentityGate kind="tutor" fieldLabel="Tutor id" title="Set up" description="desc">
        {(id) => <p>Gated content for {id}</p>}
      </IdentityGate>,
    );

    expect(screen.getByText("Gated content for 11111111-1111-1111-1111-111111111111")).toBeInTheDocument();
    expect(screen.queryByLabelText("Tutor id")).not.toBeInTheDocument();
  });
});
