/**
 * JS-side mirror of the breakpoint values in src/styles/tokens.css.
 * CSS custom properties cannot be interpolated into @media queries, so these two
 * sources must be kept in sync by hand — this is the only place that should happen.
 */
export const breakpoints = {
  sm: 480,
  md: 768,
  lg: 1024,
  xl: 1280,
} as const;

export type Breakpoint = keyof typeof breakpoints;
