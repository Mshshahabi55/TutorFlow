/**
 * `returnTo` is read directly from a public, attacker-controllable query string
 * (`/login?returnTo=...`) — it is never trustworthy just because TutorFlow's own links always
 * construct a safe one. Only a same-origin, path-relative value is ever honored; everything else
 * (an absolute URL, a protocol-relative `//host` URL, or a backslash variant some browsers
 * normalize to protocol-relative) falls back to `/`. This is the one place that validates it,
 * applied where the raw query parameter is first read (ADR-0023's "redirect-with-return-to" flow
 * assumed only TutorFlow's own code ever sets this value — it did not consider a crafted link).
 */
export function sanitizeReturnTo(raw: string | null | undefined): string {
  if (!raw) {
    return '/';
  }
  if (!raw.startsWith('/')) {
    return '/';
  }
  if (raw.startsWith('//') || raw.startsWith('/\\')) {
    return '/';
  }
  return raw;
}
