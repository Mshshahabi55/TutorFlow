import { afterEach } from "vitest";
import "@testing-library/jest-dom/vitest";

/**
 * Phase 4.9: AuthProvider now persists the authenticated session to
 * sessionStorage (surviving a page reload) rather than in-memory only.
 * jsdom's `window` — and its sessionStorage — persists across every test in
 * a file (and, depending on Vitest's pool, across files sharing a worker),
 * so a session written by one test would otherwise silently leak into the
 * next test's fresh AuthProvider mount, exactly the kind of cross-test
 * bleed CLAUDE.md's testing discipline exists to prevent. Cleared globally,
 * once, rather than requiring every test file to remember its own
 * `beforeEach` (the pattern already used ad hoc for ActorProvider's
 * localStorage key in several test files).
 */
afterEach(() => {
  window.sessionStorage.clear();
});

/**
 * jsdom implements no CSSOM layout, so window.matchMedia does not exist —
 * MUI's useMediaQuery (used by the responsive AppLayout) throws without it.
 * Defaults to "no match" (mobile-first); tests exercising a specific
 * breakpoint override window.matchMedia themselves.
 */
if (typeof window !== "undefined" && !window.matchMedia) {
  window.matchMedia = (query: string): MediaQueryList => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  });
}
