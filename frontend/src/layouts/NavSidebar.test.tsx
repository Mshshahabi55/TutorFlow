import { useEffect } from "react";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { NavSidebar } from "@/layouts/NavSidebar";
import { ActorProvider } from "@/shared/context/ActorProvider";
import { AuthProvider } from "@/shared/context/AuthProvider";
import type { AuthenticatedUser } from "@/shared/context/AuthContext";
import { useAuth } from "@/shared/hooks/useAuth";

function AuthHarness({ user }: { user: AuthenticatedUser | null }) {
  const { setUser } = useAuth();

  useEffect(() => {
    setUser(user);
  }, [setUser, user]);

  return null;
}

function renderNavSidebar(props: {
  variant: "permanent" | "temporary";
  onClose?: () => void;
  authUser?: AuthenticatedUser | null;
  initialEntry?: string;
}) {
  const onClose = props.onClose ?? vi.fn();
  render(
    <AuthProvider>
      <ActorProvider>
        <MemoryRouter initialEntries={[props.initialEntry ?? "/"]}>
          <AuthHarness user={props.authUser ?? null} />
          <NavSidebar variant={props.variant} open onClose={onClose} />
        </MemoryRouter>
      </ActorProvider>
    </AuthProvider>,
  );
  return { onClose };
}

describe("NavSidebar", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("renders the Dashboard link and the Identity & Relationship section", () => {
    renderNavSidebar({ variant: "permanent" });

    expect(screen.getByRole("link", { name: "Dashboard" })).toBeInTheDocument();
    expect(screen.getByText("Identity & Relationship")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Tutor directory" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Relationships" })).toBeInTheDocument();
  });

  it("shows every role-specific entry when no dev role is selected", () => {
    renderNavSidebar({ variant: "permanent" });

    expect(screen.getByRole("link", { name: "Register as Tutor" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Register as Student" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Pending Tutor approvals" })).toBeInTheDocument();
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
      },
    });

    expect(screen.getByRole("link", { name: "Register as Tutor" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Register as Student" })).not.toBeInTheDocument();
  });

  it("hides role-specific entries that don't match the selected dev role (display convenience only)", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "Tutor");

    renderNavSidebar({ variant: "permanent" });

    expect(screen.getByRole("link", { name: "Register as Tutor" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Register as Student" })).not.toBeInTheDocument();
    expect(
      screen.queryByRole("link", { name: "Pending Tutor approvals" }),
    ).not.toBeInTheDocument();
  });

  it("renders the Discovery and Scheduling & Booking sections", () => {
    renderNavSidebar({ variant: "permanent" });

    expect(screen.getByText("Discovery")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Search Tutors" })).toBeInTheDocument();
    expect(screen.getByText("Scheduling & Booking")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Availability Slot lookup" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Session lookup" })).toBeInTheDocument();
  });

  it("hides Scheduling entries that don't match the selected dev role", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "AdminStaff");

    renderNavSidebar({ variant: "permanent" });

    expect(screen.queryByRole("link", { name: "Declare availability" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Book a session" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Tutor sessions" })).not.toBeInTheDocument();
  });

  it("shows Marketplace Oversight entries only for the AdminStaff dev role", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "AdminStaff");

    renderNavSidebar({ variant: "permanent" });

    expect(screen.getByText("Marketplace Oversight")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Admin dashboard" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "All sessions" })).toBeInTheDocument();
  });

  it("hides Marketplace Oversight entries for a non-Admin dev role", () => {
    window.localStorage.setItem("tutorflow.devActorRole", "Tutor");

    renderNavSidebar({ variant: "permanent" });

    expect(screen.queryByRole("link", { name: "Admin dashboard" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "All sessions" })).not.toBeInTheDocument();
  });

  it("closes the drawer after navigating when rendered as temporary (mobile/tablet)", async () => {
    const { onClose } = renderNavSidebar({ variant: "temporary" });

    await userEvent.click(screen.getByRole("link", { name: "Dashboard" }));

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("does not call onClose after navigating when rendered as permanent (desktop)", async () => {
    const { onClose } = renderNavSidebar({ variant: "permanent" });

    await userEvent.click(screen.getByRole("link", { name: "Dashboard" }));

    expect(onClose).not.toHaveBeenCalled();
  });

  // Phase D2 Task 3: the same boolean drives the visual .Mui-selected
  // indicator and this accessible signal, so a screen-reader user gets an
  // unambiguous "you are here" too, not just a sighted one.
  it("marks the active route with aria-current='page', and no other link", () => {
    renderNavSidebar({ variant: "permanent", initialEntry: "/discovery/tutors/search" });

    expect(screen.getByRole("link", { name: "Search Tutors" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(screen.getByRole("link", { name: "Dashboard" })).not.toHaveAttribute("aria-current");
  });
});
