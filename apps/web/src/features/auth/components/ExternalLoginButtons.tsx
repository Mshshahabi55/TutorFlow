import { API_BASE_URL } from '@/lib/api/client';
import styles from './ExternalLoginButtons.module.css';

export interface ExternalLoginButtonsProps {
  returnTo: string;
}

/** Standard, widely-used monochrome/brand-colored glyphs (not an official asset download) —
 * matches the real Preply login page's own buttons (preply.com/en/login, directly observed
 * 2026-10-18), which show a colored provider icon beside each "Continue with …" label. */
function GoogleIcon() {
  return (
    <svg viewBox="0 0 48 48" width="18" height="18" aria-hidden="true">
      <path
        fill="#4285F4"
        d="M45.12 24.5c0-1.56-.14-3.06-.4-4.5H24v8.51h11.84c-.51 2.75-2.06 5.08-4.39 6.64v5.52h7.11c4.16-3.83 6.56-9.47 6.56-16.17z"
      />
      <path
        fill="#34A853"
        d="M24 46c5.94 0 10.92-1.97 14.56-5.33l-7.11-5.52c-1.97 1.32-4.49 2.1-7.45 2.1-5.73 0-10.58-3.87-12.31-9.07H4.34v5.7C7.96 41.07 15.4 46 24 46z"
      />
      <path fill="#FBBC05" d="M11.69 28.18A13.98 13.98 0 0 1 10.9 24c0-1.45.25-2.86.69-4.18v-5.7H4.34A21.97 21.97 0 0 0 2 24c0 3.55.85 6.91 2.34 9.88l7.35-5.7z" />
      <path
        fill="#EA4335"
        d="M24 10.75c3.23 0 6.13 1.11 8.41 3.29l6.31-6.31C34.91 4.18 29.93 2 24 2 15.4 2 7.96 6.93 4.34 14.12l7.35 5.7c1.73-5.2 6.58-9.07 12.31-9.07z"
      />
    </svg>
  );
}

function FacebookIcon() {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
      <path
        fill="#1877F2"
        d="M24 12.07C24 5.4 18.63 0 12 0S0 5.4 0 12.07C0 18.1 4.39 23.1 10.13 24v-8.44H7.08v-3.49h3.05V9.41c0-3.02 1.79-4.69 4.53-4.69 1.31 0 2.68.24 2.68.24v2.97h-1.51c-1.49 0-1.96.93-1.96 1.89v2.25h3.33l-.53 3.49h-2.8V24C19.61 23.1 24 18.1 24 12.07z"
      />
    </svg>
  );
}

/** Full-page-navigation links to the backend's existing OAuth challenge endpoints (ADR-0031,
 * `IdentityModule.MapIdentityModule` — `/api/identity/external/{google,facebook}`). Deliberately
 * plain `<a>` tags, not a mutation/fetch call: the backend responds with a redirect straight to
 * the provider's own consent screen, so the browser must actually navigate there. `returnTo` is
 * the same value already passed to `LoginForm`/`SignupForm` — the backend re-sanitizes it itself
 * (`ExternalLoginReturnTo.Sanitize`) before ever using it, so no validation is duplicated here. */
export function ExternalLoginButtons({ returnTo }: ExternalLoginButtonsProps) {
  const query = `?returnTo=${encodeURIComponent(returnTo)}`;

  return (
    <div className={styles.group}>
      <a className={styles.button} href={`${API_BASE_URL}/api/identity/external/google${query}`}>
        <GoogleIcon />
        Continue with Google
      </a>
      <a className={styles.button} href={`${API_BASE_URL}/api/identity/external/facebook${query}`}>
        <FacebookIcon />
        Continue with Facebook
      </a>

      <div className={styles.divider} role="separator">
        <span>or</span>
      </div>
    </div>
  );
}
