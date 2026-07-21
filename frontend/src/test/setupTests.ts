import "@testing-library/jest-dom/vitest";

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
