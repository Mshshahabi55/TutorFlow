/** Builds a same-origin `/login` or `/signup` href that preserves both `returnTo` and the
 * tutor-signup `context` flag across a Login<->Signup bounce. `returnTo` is passed through as-is
 * here — it is validated once, at the point each page first reads it from the URL via
 * `sanitizeReturnTo`, not re-validated on every link built from it. `context` is never anything
 * but the literal 'tutor' this module writes itself, so there is no injection/open-redirect
 * surface in either parameter. */
export function buildAuthHref(path: '/signup' | '/login', returnTo: string, context?: 'tutor'): string {
  const params = new URLSearchParams();
  if (context === 'tutor') {
    params.set('context', 'tutor');
  }
  if (returnTo !== '/') {
    params.set('returnTo', returnTo);
  }
  const query = params.toString();
  return query ? `${path}?${query}` : path;
}
