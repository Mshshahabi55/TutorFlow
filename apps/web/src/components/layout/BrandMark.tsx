/**
 * TutorFlow's brand mark — two diagonally-offset rounded squares, overlapping. An original
 * design, not a stock icon: deliberately abstract rather than a literal speech bubble, to stay
 * clearly distinct from established language-learning marks checked during design research
 * (italki's logo is a single speech-bubble outline wrapping its wordmark; Preply's is a paper
 * plane; Babbel's is a plain "+"). Also deliberately NOT two plain circles — that specific
 * horizontal-overlap shape is immediately read as Mastercard's mark worldwide. The diagonal
 * offset and rounded-square geometry read as "two things meeting/overlapping" (two people, two
 * languages, tutor and student) without tracing any of those.
 *
 * Theme-aware by design, not by prop: the back square uses `currentColor`, so it automatically
 * matches whatever color the "TutorFlow" wordmark itself is in each header variant (pink on the
 * white header, black on the pink home-page header — see SiteHeader.module.css `.logo` /
 * `.headerHome .logo`). The front square is cut out in the page background color with a
 * matching currentColor outline, so the two shapes stay visually distinct from each other
 * regardless of which background they sit on.
 */
export function BrandMark({ size = 24 }: { size?: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 28 28"
      fill="none"
      aria-hidden="true"
      style={{ flexShrink: 0 }}
    >
      <rect x="2" y="2" width="16" height="16" rx="5" fill="currentColor" />
      <rect
        x="10"
        y="10"
        width="16"
        height="16"
        rx="5"
        fill="var(--color-bg-surface)"
        stroke="currentColor"
        strokeWidth="2"
      />
    </svg>
  );
}
