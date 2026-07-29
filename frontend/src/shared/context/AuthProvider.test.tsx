import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { AuthProvider } from "@/shared/context/AuthProvider";
import { useAuth } from "@/shared/hooks/useAuth";

const SESSION_STORAGE_KEY = "tutorflow.authSession";

const VALID_USER = {
  token: "raw-token",
  accountId: "11111111-1111-1111-1111-111111111111",
  role: "Student",
  expiresAtUtc: "2999-01-01T00:00:00Z",
  email: "student@example.com",
};

function Probe() {
  const { user, isAuthenticated } = useAuth();
  return (
    <div>
      <span>isAuthenticated: {String(isAuthenticated)}</span>
      <span>role: {user?.role ?? "none"}</span>
    </div>
  );
}

describe("AuthProvider session persistence (Phase 4.9)", () => {
  it("starts signed out when sessionStorage carries no session", () => {
    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    expect(screen.getByText("isAuthenticated: false")).toBeInTheDocument();
  });

  // Simulates surviving a page reload: a fresh AuthProvider mount (the same
  // thing a hard refresh produces) reads whatever a prior mount already
  // wrote to sessionStorage — proving finding #1 ("the Sign in button
  // becomes active again") no longer happens for a page reload.
  it("restores a still-valid session from sessionStorage on a fresh mount", () => {
    window.sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(VALID_USER));

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    expect(screen.getByText("isAuthenticated: true")).toBeInTheDocument();
    expect(screen.getByText("role: Student")).toBeInTheDocument();
  });

  it("discards an expired stored session rather than silently restoring it", () => {
    window.sessionStorage.setItem(
      SESSION_STORAGE_KEY,
      JSON.stringify({ ...VALID_USER, expiresAtUtc: "2020-01-01T00:00:00Z" }),
    );

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    expect(screen.getByText("isAuthenticated: false")).toBeInTheDocument();
    expect(window.sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it("discards a stored session missing the email field (pre-RC4.3 shape) rather than restoring it half-formed", () => {
    const legacyShape = {
      token: VALID_USER.token,
      accountId: VALID_USER.accountId,
      role: VALID_USER.role,
      expiresAtUtc: VALID_USER.expiresAtUtc,
    };
    window.sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(legacyShape));

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    expect(screen.getByText("isAuthenticated: false")).toBeInTheDocument();
  });

  it("discards a malformed stored session rather than throwing", () => {
    window.sessionStorage.setItem(SESSION_STORAGE_KEY, "{not json");

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    expect(screen.getByText("isAuthenticated: false")).toBeInTheDocument();
  });

  it("never persists to localStorage (docs/adr/ADR-017-authentication-mechanism-decision.md)", () => {
    window.sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(VALID_USER));

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );

    expect(window.localStorage.length).toBe(0);
  });
});
