/**
 * Small inline-SVG national flags, rendered directly instead of as a Unicode regional-indicator
 * emoji pair (`lib/countryFlag.ts`'s old approach) — Windows ships no flag glyphs in its default
 * emoji font, so Chromium on Windows falls back to rendering the two letters themselves (e.g.
 * literal text "GB") instead of a flag. An SVG renders identically on every OS/browser.
 *
 * Simplified vector reproductions of public national flags (not copyrighted/proprietary assets),
 * covering exactly the fixed country set already supported by the app
 * (`features/search/constants.ts`'s `COUNTRY_OPTIONS`). Unknown codes render nothing so a bad
 * value never shows a broken flag.
 */
import { useId, type JSX } from 'react';

export type SupportedFlagCode = 'ES' | 'FR' | 'DE' | 'GB' | 'US' | 'PT' | 'IT' | 'MX';

const FLAGS: Record<SupportedFlagCode, (titleId: string) => JSX.Element> = {
  FR: (titleId) => (
    <svg viewBox="0 0 30 20" role="img" aria-labelledby={titleId}>
      <title id={titleId}>France</title>
      <rect width="30" height="20" fill="#ED2939" />
      <rect width="20" height="20" fill="#fff" />
      <rect width="10" height="20" fill="#002395" />
    </svg>
  ),
  DE: (titleId) => (
    <svg viewBox="0 0 30 20" role="img" aria-labelledby={titleId}>
      <title id={titleId}>Germany</title>
      <rect width="30" height="20" fill="#FFCE00" />
      <rect width="30" height="13.34" fill="#000" />
      <rect width="30" height="6.67" fill="#DD0000" />
    </svg>
  ),
  IT: (titleId) => (
    <svg viewBox="0 0 30 20" role="img" aria-labelledby={titleId}>
      <title id={titleId}>Italy</title>
      <rect width="30" height="20" fill="#CE2B37" />
      <rect width="20" height="20" fill="#fff" />
      <rect width="10" height="20" fill="#009246" />
    </svg>
  ),
  MX: (titleId) => (
    <svg viewBox="0 0 30 20" role="img" aria-labelledby={titleId}>
      <title id={titleId}>Mexico</title>
      <rect width="30" height="20" fill="#CE1126" />
      <rect width="20" height="20" fill="#fff" />
      <rect width="10" height="20" fill="#006341" />
      <circle cx="15" cy="10" r="2.6" fill="#8B5A2B" />
    </svg>
  ),
  PT: (titleId) => (
    <svg viewBox="0 0 30 20" role="img" aria-labelledby={titleId}>
      <title id={titleId}>Portugal</title>
      <rect width="30" height="20" fill="#DA020E" />
      <rect width="12" height="20" fill="#006600" />
      <circle cx="12" cy="10" r="3.2" fill="#FFCC00" stroke="#DA020E" strokeWidth="0.6" />
    </svg>
  ),
  ES: (titleId) => (
    <svg viewBox="0 0 30 20" role="img" aria-labelledby={titleId}>
      <title id={titleId}>Spain</title>
      <rect width="30" height="20" fill="#AA151B" />
      <rect y="5" width="30" height="10" fill="#F1BF00" />
    </svg>
  ),
  GB: (titleId) => (
    <svg viewBox="0 0 30 20" role="img" aria-labelledby={titleId}>
      <title id={titleId}>United Kingdom</title>
      <rect width="30" height="20" fill="#00247D" />
      <path d="M0 0 L30 20 M30 0 L0 20" stroke="#fff" strokeWidth="4" />
      <path d="M0 0 L30 20 M30 0 L0 20" stroke="#CF142B" strokeWidth="1.6" />
      <path d="M15 0 V20 M0 10 H30" stroke="#fff" strokeWidth="6.6" />
      <path d="M15 0 V20 M0 10 H30" stroke="#CF142B" strokeWidth="4" />
    </svg>
  ),
  US: (titleId) => (
    <svg viewBox="0 0 30 20" role="img" aria-labelledby={titleId}>
      <title id={titleId}>United States</title>
      <rect width="30" height="20" fill="#fff" />
      {[0, 2, 4, 6, 8, 10, 12].map((y) => (
        <rect key={y} y={y} width="30" height="1.54" fill="#B22234" />
      ))}
      <rect width="13" height="10.8" fill="#3C3B6E" />
    </svg>
  ),
};

export const SUPPORTED_FLAG_CODES = Object.keys(FLAGS) as SupportedFlagCode[];

export function isSupportedFlagCode(code: string): code is SupportedFlagCode {
  return code.toUpperCase() in FLAGS;
}

export interface FlagIconProps {
  code: string;
  size?: number;
  className?: string;
}

/** Renders a small flag for a supported ISO 3166-1 alpha-2 code, or nothing for an unsupported one. */
export function FlagIcon({ code, size = 16, className }: FlagIconProps) {
  const titleId = useId();
  const upper = code.toUpperCase();
  if (!isSupportedFlagCode(upper)) {
    return null;
  }

  const render = FLAGS[upper];

  return (
    <span
      className={className}
      style={{
        display: 'inline-block',
        verticalAlign: 'middle',
        width: size,
        height: (size * 2) / 3,
        borderRadius: 2,
        overflow: 'hidden',
        flexShrink: 0,
        boxShadow: 'inset 0 0 0 1px rgba(0,0,0,0.12)',
      }}
    >
      {render(titleId)}
    </span>
  );
}
