import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { NavSidebar } from "@/layouts/NavSidebar";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { AuthProvider } from "@/shared/context/AuthProvider";
import type { AuthenticatedUser } from "@/shared/context/AuthContext";
import { AuthHarness } from "@/test/AuthHarness";

function renderNavSidebar(props: {
  variant: "permanent" | "temporary";
  onClose?: () => void;
  authUser?: AuthenticatedUser | null;
  initialEntry?: string;
  collapsed?: boolean;
  onToggleCollapse?: () => void;
}) {
  const onClose = props.onClose ?? vi.fn();
  const onToggleCollapse = props.onToggleCollapse ?? vi.fn();
  render(
    <AuthProvider>
      <ActorProvider>
        <MemoryRouter initialEntries={[props.initialEntry ?? "/"]}>
          {props.authUser ? <AuthHarness user={props.authUser} /> : null}
          <NavSidebar
            variant={props.variant}
            open
            onClose={onClose}
            collapsed={props.collapsed}
            onToggleCollapse={onToggleCollapse}
          />
        </MemoryRouter>
      </ActorProvider>
    </AuthProvider>,
  );
  return { onClose, onToggleCollapse };
}

// RC2: the nav is now a short, flat, per-role list (Home/Find Tutors/My
// Lessons/Messages/Profile for a Student, etc.) instead of a shared list
// grouped under bounded-context section titles — a consumer marketplace's
// IA, not an internal admin panel's.
describe("NavSidebar", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("shows the Student's flat 6-item nav — Home, Find Tutors, Favorites, My Lessons, Messages, Profile", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "Student");

    renderNavSidebar({ variant: "permanent" });

    expect(screen.getByRole("link", { name: "Home" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "Find Tutors" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Favorites" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "My Lessons" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Messages" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Profile" })).toBeInTheDocument();
    expect(screen.getAllByRole("link")).toHaveLength(6);
  });

  it("shows the Tutor's flat nav — Dashboard, My Students, My Lessons, Availability, Profile", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "Tutor");

    renderNavSidebar({ variant: "permanent" });

    expect(screen.getByRole("link", { name: "Dashboard" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "My Students" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "My Lessons" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Availability" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Profile" })).toBeInTheDocument();
  });

  it("shows Admin's own separate operations nav, not the marketplace nav", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "AdminStaff");

    renderNavSidebar({ variant: "permanent" });

    expect(screen.getByRole("link", { name: "Operations" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Tutor Approvals" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "All Sessions" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Find Tutors" })).not.toBeInTheDocument();
  });

  it("shows a minimal nav with no role selected", () => {
    renderNavSidebar({ variant: "permanent" });

    expect(screen.getByRole("link", { name: "Home" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Find Tutors" })).toBeInTheDocument();
    expect(screen.getAllByRole("link")).toHaveLength(2);
  });

  it("prefers the authenticated user role over the dev preview role for navigation", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "Student");

    renderNavSidebar({
      variant: "permanent",
      authUser: {
        token: "token",
        accountId: "account-1",
        role: "Tutor",
        expiresAtUtc: "2026-07-21T00:00:00Z",
        email: "tutor@example.com",
      },
    });

    expect(screen.getByRole("link", { name: "My Students" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Find Tutors" })).not.toBeInTheDocument();
  });

  it("closes the drawer after navigating when rendered as temporary (mobile/tablet)", async () => {
    const { onClose } = renderNavSidebar({ variant: "temporary" });

    await userEvent.click(screen.getByRole("link", { name: "Home" }));

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("does not call onClose after navigating when rendered as permanent (desktop)", async () => {
    const { onClose } = renderNavSidebar({ variant: "permanent" });

    await userEvent.click(screen.getByRole("link", { name: "Home" }));

    expect(onClose).not.toHaveBeenCalled();
  });

  // Phase D2 Task 3: the same boolean drives the visual .Mui-selected
  // indicator and this accessible signal, so a screen-reader user gets an
  // unambiguous "you are here" too, not just a sighted one.
  it("marks the active route with aria-current='page', and no other link", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "Student");
    renderNavSidebar({ variant: "permanent", initialEntry: "/discovery/tutors/search" });

    expect(screen.getByRole("link", { name: "Find Tutors" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(screen.getByRole("link", { name: "Home" })).not.toHaveAttribute("aria-current");
  });

  // Phase D4: icon-rail collapse (permanent/desktop only).
  describe("collapse (Phase D4)", () => {
    it("shows the brand and a Collapse navigation button when expanded, permanent", () => {
      renderNavSidebar({ variant: "permanent", collapsed: false });

      expect(screen.getByText("TutorFlow")).toBeInTheDocument();
      expect(screen.getByLabelText("Collapse navigation")).toBeInTheDocument();
    });

    it("calls onToggleCollapse when the collapse button is clicked", async () => {
      const { onToggleCollapse } = renderNavSidebar({ variant: "permanent", collapsed: false });

      await userEvent.click(screen.getByLabelText("Collapse navigation"));

      expect(onToggleCollapse).toHaveBeenCalledTimes(1);
    });

    it("hides labels and the brand, keeps links reachable, when collapsed", () => {
      renderNavSidebar({ variant: "permanent", collapsed: true });

      expect(screen.queryByText("TutorFlow")).not.toBeInTheDocument();
      expect(screen.queryByText("Home")).not.toBeInTheDocument();
      expect(screen.getByLabelText("Expand navigation")).toBeInTheDocument();
      // Links remain in the DOM (icon-only), still reachable/labelled via Tooltip.
      expect(screen.getAllByRole("link").length).toBeGreaterThan(0);
    });

    it("never shows a collapse toggle for the temporary (mobile) drawer", () => {
      renderNavSidebar({ variant: "temporary", collapsed: false });

      expect(screen.queryByLabelText("Collapse navigation")).not.toBeInTheDocument();
      expect(screen.queryByLabelText("Expand navigation")).not.toBeInTheDocument();
    });
  });
});
