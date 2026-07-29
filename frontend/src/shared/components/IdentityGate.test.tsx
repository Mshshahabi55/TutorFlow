import { beforeEach, describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { IdentityGate } from "@/shared/components/IdentityGate";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { AuthHarness } from "@/test/AuthHarness";
import type { AuthenticatedUser } from "@/shared/context/AuthContext";

function renderGate(kind: "tutor" | "student" | "parentGuardian", authUser?: AuthenticatedUser) {
  return render(
    <MemoryRouter>
      <AuthProvider>
        {authUser ? <AuthHarness user={authUser} /> : null}
        <IdentityGate kind={kind} fieldLabel={`${kind} id`} title="Let's find your lessons" description="One-time setup.">
          {(id) => <p>Gated content for {id}</p>}
        </IdentityGate>
      </AuthProvider>
    </MemoryRouter>,
  );
}

const TUTOR_USER: AuthenticatedUser = {
  token: "t",
  accountId: "11111111-1111-1111-1111-111111111111",
  role: "Tutor",
  expiresAtUtc: "2999-01-01T00:00:00Z",
  email: "tutor@example.com",
};

describe("IdentityGate", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("shows the friendly setup prompt, never a bare id form, when signed out with nothing remembered yet", () => {
    renderGate("student");

    expect(screen.getByRole("heading", { name: "Let's find your lessons" })).toBeInTheDocument();
    expect(screen.getByText("One-time setup.")).toBeInTheDocument();
    expect(screen.getByLabelText("student id")).toBeInTheDocument();
    expect(screen.queryByText(/Gated content/)).not.toBeInTheDocument();
  });

  it("remembers the id after one entry and renders the gated content immediately on the next mount (dev preview, signed out)", async () => {
    const { unmount } = renderGate("tutor");

    await userEvent.type(screen.getByLabelText("tutor id"), "11111111-1111-1111-1111-111111111111");
    await userEvent.click(screen.getByRole("button", { name: "Look up" }));

    expect(await screen.findByText("Gated content for 11111111-1111-1111-1111-111111111111")).toBeInTheDocument();

    unmount();
    renderGate("tutor");

    expect(screen.getByText("Gated content for 11111111-1111-1111-1111-111111111111")).toBeInTheDocument();
    expect(screen.queryByLabelText("tutor id")).not.toBeInTheDocument();
  });

  it("resolves a real signed-in Tutor's own id automatically — never shows the id-entry form", async () => {
    renderGate("tutor", TUTOR_USER);

    expect(await screen.findByText(`Gated content for ${TUTOR_USER.accountId}`)).toBeInTheDocument();
    expect(screen.queryByLabelText("tutor id")).not.toBeInTheDocument();
  });

  it("shows a friendly 'couldn't load your profile' state for a signed-in role that doesn't match this gate's kind", async () => {
    renderGate("student", TUTOR_USER);

    expect(await screen.findByRole("heading", { name: "We couldn't load your profile" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Go to Home" })).toBeInTheDocument();
    expect(screen.queryByLabelText("student id")).not.toBeInTheDocument();
    expect(screen.queryByText(/Gated content/)).not.toBeInTheDocument();
  });
});
