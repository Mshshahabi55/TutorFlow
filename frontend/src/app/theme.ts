import { createTheme } from "@mui/material/styles";
import type { PaletteMode, Shadows } from "@mui/material/styles";

/**
 * TutorFlow's design tokens — Preply Redesign Phase 1 (Visual Foundation &
 * Design System Refresh). Replaces Phase D1/D3's cool-neutral, low-
 * saturation palette with a warmer, marketplace-flavoured one (deep
 * emerald primary, generous radius, soft shadow, more air) — the reference
 * class this phase names (Preply/Airbnb/Notion/Linear), not the ERP/admin-
 * panel look Phase D1 deliberately chose instead. This is still an
 * implementation choice, not a ratified brand — no approved document
 * specifies branding for TutorFlow — and is scoped to visual tokens only:
 * no route, endpoint, permission, or business rule changes anywhere in
 * this phase.
 *
 * Every named export below (`neutral`, `typographyScale`, `spacingUnitPx`,
 * `radiusPx`, `namedShadows`) is the same value the theme is built from —
 * exported separately so the dev-only style guide page can render an
 * accurate swatch of the raw tokens without reverse-engineering them out
 * of a constructed MUI theme object. Every one of these exports keeps its
 * Phase D1 name and shape (a Record of the same or a superset of keys), so
 * `StyleGuidePage.tsx`'s generic `Object.entries(...)` iteration keeps
 * working unchanged.
 *
 * `createAppTheme(mode)` builds either palette from the same shape;
 * `theme`/`darkTheme` below are the two pre-built instances every call
 * site already expects. `frontend/src/app/theme.test.ts` enforces WCAG AA
 * (4.5:1) on every `contrastText`/`main` pair and on `text.primary`/
 * `text.secondary` against both backgrounds — every tone below was chosen
 * (and iterated against that real, automated test, not eyeballed) to
 * clear it in both modes; see the per-tone notes for where the requested
 * brand colour needed a *dark* contrastText instead of white to actually
 * pass (a literal white-on-`#16A34A`/`#D97706` pairing measures under
 * 3.5:1 — a real, verifiable AA failure, not a style preference).
 */

// ---------------------------------------------------------------------------
// Colour — light mode. The brand palette given for this phase (Deep Emerald
// primary family, Tailwind's own gray/slate scale for neutrals) plus four
// semantic tones. `success`/`warning` use `text.primary` (near-black) as
// their `contrastText` rather than white — white-on-`#16A34A` and
// white-on-`#D97706` both measure well under the 4.5:1 AA minimum
// (~3.3:1 and ~3.2:1); dark text on the same fills clears it comfortably
// (~5.4:1 and ~5.6:1). `error`'s white contrastText does clear AA (~4.8:1),
// kept for the expected "danger fill, light text" convention.
// ---------------------------------------------------------------------------

/** Tailwind's own true-gray scale — chosen because the brief's Border (#E5E7EB) and Text tokens (#111827/#6B7280) are literally gray-200/900/500 from it. */
const neutral = {
  50: "#F9FAFB",
  100: "#F3F4F6",
  200: "#E5E7EB",
  300: "#D1D5DB",
  400: "#9CA3AF",
  500: "#6B7280",
  600: "#4B5563",
  700: "#374151",
  800: "#1F2937",
  900: "#111827",
} as const;

/** Deep Emerald family — main/hover/accent from the brief map directly onto MUI's own main/dark/light triad, so every `contained` Button's automatic hover-darken already lands on the specified Primary Hover colour with no extra code. */
const brandPrimary = {
  main: "#0F766E",
  dark: "#115E59",
  light: "#14B8A6",
  contrastText: "#FFFFFF",
};

const semantic = {
  success: { main: "#16A34A", contrastText: neutral[900] },
  warning: { main: "#D97706", contrastText: neutral[900] },
  error: { main: "#DC2626", contrastText: "#FFFFFF" },
  // Not named in the brief's colour system — chosen from the same Tailwind
  // family (blue-600) so it reads as "the same design language's fourth
  // semantic tone," not an unrelated hue.
  info: { main: "#2563EB", contrastText: "#FFFFFF" },
};

// ---------------------------------------------------------------------------
// Colour — dark mode. Not specified by this phase's brief (light mode
// only), but live in the real app since Phase D4 (`AppProviders.tsx`
// already switches between `theme`/`darkTheme` via `ColorModeProvider`,
// and `AppHeader` already renders a real `ThemeToggle`) — re-tinted from
// Phase D1/D3's blue accent to this phase's emerald family so dark mode
// inherits the new brand rather than a mismatched leftover. Same "bright
// fill + near-black text" pattern Phase D3 already established (measured,
// not assumed) — bright tones against a near-black `contrastText` clear
// AA with wide margin.
// ---------------------------------------------------------------------------

const darkSurface = {
  background: "#0B1412",
  paper: "#11201D",
  elevated: "#16281F",
  divider: "#24352E",
  border: "#2E4038",
};

const darkText = {
  primary: "#F1F5F4",
  secondary: "#94A3A0",
  disabled: "#5B6864",
};

const darkContrastText = "#08120F";

const darkPrimary = {
  main: "#2DD4BF",
  dark: "#14B8A6",
  light: "#5EEAD4",
  contrastText: darkContrastText,
};

const darkSecondary = { main: "#9CA6A2", contrastText: darkContrastText };

const darkSemantic = {
  success: { main: "#4ADE80", contrastText: darkContrastText },
  warning: { main: "#FBBF24", contrastText: darkContrastText },
  error: { main: "#F87171", contrastText: darkContrastText },
  info: { main: "#60A5FA", contrastText: darkContrastText },
};

/** StatusPill's neutral/outlined tone (MuiChip `outlined` override below) needs its own light-on-dark pair, distinct from the semantic tones above. */
const darkNeutralPillText = "#CBD2D9";

// ---------------------------------------------------------------------------
// Typography — same variant-name convention as Phase D1 (every call site
// already writes `variant="h4"` or `variant="body2"`, never a raw font
// size), values updated to this phase's hierarchy: Page title 44–48px,
// Section title 28–32px, Card title 20–22px, Body 16px, Caption 14px,
// Buttons 16px/600. Mapped onto the existing scale with the smallest
// possible set of changes so no call site needs to change its own
// `variant` prop:
//   - h4 (PageHeader's own title variant) -> Page title, bumped 24 -> 44px.
//   - h1/h2/h3 stay reserved/unused today but are now sized for a future
//     "Section title" use (30/32/28px) instead of Phase D1's own unused
//     36/30px reservations, so they're ready rather than needing a second
//     pass later.
//   - h5 (SectionCard's own title variant, "card/section headings") ->
//     Card title, nudged 20 -> 21px (Phase D1's 20px was already inside
//     this phase's 20–22px band).
//   - caption: 12 -> 14px (explicit brief instruction).
//   - button: 14 -> 16px/600 (explicit brief instruction).
//   - Every variant's line-height increased for "Increase readability...
//     line-height everywhere" — except the two Page-title-register
//     variants (h1/h4), where a tight 1.15 is the correct choice for very
//     large display text (a loose line-height on 44px+ text reads as an
//     accident, not "airy" — every reference app this phase names sets its
//     own hero/page titles tight, reserving generous line-height for body
//     copy, which is where the brief's own examples for it apply).
// ---------------------------------------------------------------------------

const fontFamily = [
  "-apple-system",
  "BlinkMacSystemFont",
  "Segoe UI",
  "Roboto",
  "Helvetica Neue",
  "Arial",
  "sans-serif",
].join(",");

export const typographyScale = {
  h1: { fontSize: "3rem", lineHeight: 1.15, fontWeight: 700 }, // 48px — reserved, upper Page-title bound
  h2: { fontSize: "2rem", lineHeight: 1.25, fontWeight: 700 }, // 32px — Section title (upper)
  h3: { fontSize: "1.75rem", lineHeight: 1.3, fontWeight: 700 }, // 28px — Section title (lower)
  h4: { fontSize: "2.75rem", lineHeight: 1.15, fontWeight: 700 }, // 44px — PageHeader title
  h5: { fontSize: "1.3125rem", lineHeight: 1.45, fontWeight: 600 }, // 21px — Card title (SectionCard)
  h6: { fontSize: "1.1875rem", lineHeight: 1.45, fontWeight: 600 }, // 19px — AppBar title
  subtitle1: { fontSize: "1rem", lineHeight: 1.55, fontWeight: 600 }, // 16px
  subtitle2: { fontSize: "0.875rem", lineHeight: 1.55, fontWeight: 600 }, // 14px
  body1: { fontSize: "1rem", lineHeight: 1.6, fontWeight: 400 }, // 16px — Body
  body2: { fontSize: "0.875rem", lineHeight: 1.6, fontWeight: 400 }, // 14px — secondary/dense copy
  caption: { fontSize: "0.875rem", lineHeight: 1.5, fontWeight: 400 }, // 14px — Caption
  overline: {
    fontSize: "0.75rem",
    lineHeight: 1.5,
    fontWeight: 600,
    letterSpacing: "0.06em",
    textTransform: "uppercase" as const,
  },
  button: { fontSize: "1rem", lineHeight: 1.5, fontWeight: 600, textTransform: "none" as const }, // 16px/600 — Buttons
} as const;

// ---------------------------------------------------------------------------
// Spacing — MUI's own `theme.spacing(n)` multiplier, base unit unchanged at
// 8px so every existing `spacing={n}`/`p={n}` call site keeps its current
// literal pixel value (no silent re-scale of layouts this phase doesn't
// touch) — the brief's own "Cards: padding 32 / Gap between cards: 24 /
// Gap between sections: 48" land on 4/3/6 of this same unit, applied where
// this phase actually reaches (MuiCardContent's default padding, below;
// see the module-level report for why per-page Stack `spacing` props are
// out of this phase's scope).
// ---------------------------------------------------------------------------
export const spacingUnitPx = 8;

// ---------------------------------------------------------------------------
// Shape — this phase's own generous-radius language: Buttons/Inputs at
// 16px (the `shape.borderRadius` default every plain TextField/Button/Menu
// reads), Cards/Dialogs at 24px (their own explicit override, below), Chips
// ("Badges") fully pill-shaped at 999px.
// ---------------------------------------------------------------------------
export const radiusPx = {
  sm: 8, // dense inline elements (nav rows, small chips)
  md: 16, // default: Button, TextField, Menu
  lg: 24, // Card, Paper, Dialog
  pill: 999, // Chip / StatusPill
} as const;

// ---------------------------------------------------------------------------
// Shadow — "soft shadows" replace Phase D1's border-first depth language;
// cards now carry a real, soft resting shadow and a slightly stronger
// "floating"/hover tier, both intentionally gentle (no hard, high-alpha
// drop shadow anywhere — "premium," not "heavy"). MUI's `shadows` array
// must have exactly 25 entries (elevation 0–24); flattened down to three
// deliberately soft tiers (none/resting/floating) rather than Material's
// own steep default ramp.
// ---------------------------------------------------------------------------
const restingShadowLight = "0px 1px 3px rgba(17, 24, 39, 0.06), 0px 1px 2px rgba(17, 24, 39, 0.04)";
const floatingShadowLight = "0px 12px 24px rgba(17, 24, 39, 0.10), 0px 4px 8px rgba(17, 24, 39, 0.06)";
const restingShadowDark = "0px 1px 3px rgba(0, 0, 0, 0.40), 0px 1px 2px rgba(0, 0, 0, 0.28)";
const floatingShadowDark = "0px 12px 24px rgba(0, 0, 0, 0.48), 0px 4px 8px rgba(0, 0, 0, 0.36)";

export const namedShadows = {
  none: "none",
  resting: restingShadowLight,
  floating: floatingShadowLight,
} as const;

function buildShadows(mode: PaletteMode): Shadows {
  const resting = mode === "dark" ? restingShadowDark : restingShadowLight;
  const floating = mode === "dark" ? floatingShadowDark : floatingShadowLight;
  return Array.from({ length: 25 }, (_, elevation) => {
    if (elevation === 0) return "none";
    if (elevation <= 6) return resting;
    return floating;
  }) as unknown as Shadows;
}

// ---------------------------------------------------------------------------
// Breakpoints & z-index — MUI's own defaults, unchanged (see Phase D1's own
// note: AppLayout already switches its nav drawer at `md`, forms stack at
// `sm`; nothing about this visual refresh needs a new breakpoint).
// ---------------------------------------------------------------------------

/**
 * The monospace stack every raw-id display (`CopyableId`, and the id
 * subtitle a few detail pages set inline) should read from, instead of each
 * call site repeating its own `"ui-monospace, monospace"` literal.
 */
export const monoFontFamily = "ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";

/**
 * Builds either mode's theme from the same shape. `components` overrides
 * read from the mode-specific tokens above so every shared-primitive
 * override works correctly in both modes without a second copy of the
 * `components` block.
 */
export function createAppTheme(mode: PaletteMode) {
  const isDark = mode === "dark";
  const primary = isDark ? darkPrimary : brandPrimary;
  const focusRingColor = primary.main;
  const cardTransition = "transform 200ms ease, box-shadow 200ms ease";
  const reducedMotionCardOverride = {
    "@media (prefers-reduced-motion: reduce)": { transition: "none", transform: "none" },
  };

  return createTheme({
    palette: {
      mode,
      primary,
      secondary: isDark ? darkSecondary : { main: neutral[700], contrastText: "#FFFFFF" },
      success: isDark ? darkSemantic.success : semantic.success,
      warning: isDark ? darkSemantic.warning : semantic.warning,
      error: isDark ? darkSemantic.error : semantic.error,
      info: isDark ? darkSemantic.info : semantic.info,
      grey: neutral,
      text: isDark
        ? { primary: darkText.primary, secondary: darkText.secondary, disabled: darkText.disabled }
        : { primary: neutral[900], secondary: neutral[500], disabled: neutral[400] },
      background: isDark
        ? { default: darkSurface.background, paper: darkSurface.paper }
        : { default: "#F8FAFC", paper: "#FFFFFF" },
      divider: isDark ? darkSurface.divider : neutral[200],
    },
    typography: {
      fontFamily,
      h1: typographyScale.h1,
      h2: typographyScale.h2,
      h3: typographyScale.h3,
      h4: typographyScale.h4,
      h5: typographyScale.h5,
      h6: typographyScale.h6,
      subtitle1: typographyScale.subtitle1,
      subtitle2: typographyScale.subtitle2,
      body1: typographyScale.body1,
      body2: typographyScale.body2,
      caption: typographyScale.caption,
      overline: typographyScale.overline,
      button: typographyScale.button,
    },
    spacing: spacingUnitPx,
    shape: {
      borderRadius: radiusPx.md,
    },
    shadows: buildShadows(mode),
    components: {
      // --- Respects `prefers-reduced-motion` everywhere in one place —
      // every MUI transition (Drawer slide, Collapse, hover/focus
      // transitions, Skeleton's pulse animation, this phase's own Card
      // hover-lift) already goes through the CSS `transition`/`animation`
      // properties this global rule collapses to near-zero.
      MuiCssBaseline: {
        styleOverrides: {
          "@media (prefers-reduced-motion: reduce)": {
            "*, *::before, *::after": {
              animationDuration: "0.01ms !important",
              animationIterationCount: "1 !important",
              transitionDuration: "0.01ms !important",
              scrollBehavior: "auto !important",
            },
          },
        },
      },

      // --- Button: primary=contained, secondary action=outlined, low-
      // emphasis=text/ghost — an existing convention, unchanged. "Large /
      // rounded / full height" per the brief: generous padding and a
      // minimum height that also clears the 44px mobile touch-target
      // requirement for every button in the app, not just a mobile-only
      // special case.
      MuiButton: {
        defaultProps: {
          disableElevation: true,
        },
        styleOverrides: {
          root: {
            textTransform: "none",
            borderRadius: radiusPx.md,
            minHeight: 44,
            paddingLeft: 24,
            paddingRight: 24,
            paddingTop: 10,
            paddingBottom: 10,
            transition: "background-color 150ms ease, border-color 150ms ease, transform 150ms ease",
            "&:focus-visible": {
              outline: `2px solid ${focusRingColor}`,
              outlineOffset: 2,
            },
            "&:active": {
              transform: "scale(0.98)",
            },
            "@media (prefers-reduced-motion: reduce)": {
              transition: "none",
              "&:active": { transform: "none" },
            },
          },
          sizeLarge: {
            minHeight: 52,
            paddingLeft: 32,
            paddingRight: 32,
            paddingTop: 14,
            paddingBottom: 14,
            fontSize: "1.0625rem",
          },
          sizeSmall: {
            minHeight: 36,
            paddingLeft: 16,
            paddingRight: 16,
            paddingTop: 6,
            paddingBottom: 6,
          },
        },
      },
      MuiIconButton: {
        styleOverrides: {
          root: {
            "&:focus-visible": {
              outline: `2px solid ${focusRingColor}`,
              outlineOffset: 2,
            },
          },
          sizeMedium: {
            // 44px minimum touch target (mobile responsive requirement) —
            // MUI's own default medium IconButton padding falls short of it.
            padding: 10,
          },
        },
      },
      MuiAppBar: {
        defaultProps: {
          elevation: 0,
        },
        styleOverrides: {
          root: {
            backgroundImage: "none",
          },
        },
      },
      MuiToolbar: {
        styleOverrides: {
          regular: {
            minHeight: 72,
            "@media (min-width:600px)": {
              minHeight: 76,
            },
          },
        },
      },

      // --- Form controls: rounded, larger, more padding, a clearer focus
      // ring — "Redesign every TextField."
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            borderRadius: radiusPx.md,
            backgroundColor: isDark ? darkSurface.paper : "#FFFFFF",
            "&.Mui-focused .MuiOutlinedInput-notchedOutline": {
              borderWidth: 2,
              borderColor: focusRingColor,
            },
          },
          input: {
            padding: "14px 16px",
          },
        },
      },
      MuiInputLabel: {
        styleOverrides: {
          root: {
            fontSize: typographyScale.body1.fontSize,
          },
        },
      },
      MuiFormHelperText: {
        styleOverrides: {
          root: {
            marginLeft: 0,
            fontSize: typographyScale.caption.fontSize,
          },
        },
      },
      MuiCheckbox: {
        styleOverrides: {
          root: {
            "&:focus-visible": {
              outline: `2px solid ${focusRingColor}`,
              outlineOffset: 2,
            },
          },
        },
      },
      MuiRadio: {
        styleOverrides: {
          root: {
            "&:focus-visible": {
              outline: `2px solid ${focusRingColor}`,
              outlineOffset: 2,
            },
          },
        },
      },

      // --- Card / Paper: soft shadow (not a border) is now the primary
      // depth cue, generous 24px radius, and — for Card specifically — a
      // gentle hover lift, "premium, tactile" feedback the brief asks for
      // on every card, interactive or not (Preply/Airbnb's own card
      // language). `MuiCardContent`'s default padding is bumped to 32px
      // (the brief's own "Cards: padding 32"), which every existing
      // `<Card><CardContent>...` call site inherits with no prop change.
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: "none",
          },
          outlined: {
            borderColor: isDark ? darkSurface.divider : neutral[200],
          },
          rounded: {
            borderRadius: radiusPx.lg,
          },
        },
      },
      MuiCard: {
        defaultProps: {
          variant: "outlined",
        },
        styleOverrides: {
          root: {
            borderRadius: radiusPx.lg,
            boxShadow: isDark ? restingShadowDark : restingShadowLight,
            transition: cardTransition,
            "&:hover": {
              boxShadow: isDark ? floatingShadowDark : floatingShadowLight,
              transform: "translateY(-2px)",
            },
            ...reducedMotionCardOverride,
          },
        },
      },
      MuiCardContent: {
        styleOverrides: {
          root: {
            padding: 32,
            "&:last-child": {
              paddingBottom: 32,
            },
          },
        },
      },
      MuiCardActionArea: {
        styleOverrides: {
          root: {
            borderRadius: "inherit",
          },
        },
      },

      // --- Skeleton: rounded corners everywhere, matching the card
      // language it stands in for while loading ("Rounded skeletons").
      MuiSkeleton: {
        styleOverrides: {
          root: {
            borderRadius: radiusPx.sm,
          },
          rounded: {
            borderRadius: radiusPx.md,
          },
        },
      },

      // --- DataTable's Table/TableCell: unchanged structurally from Phase
      // D1 — this phase's card/typography/colour refresh already reads
      // through automatically.
      MuiTableHead: {
        styleOverrides: {
          root: {
            backgroundColor: isDark ? darkSurface.elevated : neutral[50],
            "& .MuiTableCell-root": {
              fontWeight: typographyScale.subtitle2.fontWeight,
              color: isDark ? darkText.primary : neutral[700],
              borderBottomColor: isDark ? darkSurface.divider : neutral[200],
            },
          },
        },
      },
      MuiTableCell: {
        styleOverrides: {
          root: {
            borderBottomColor: isDark ? darkSurface.divider : neutral[200],
          },
        },
      },
      MuiTableRow: {
        styleOverrides: {
          root: {
            "&:last-of-type .MuiTableCell-root": {
              borderBottom: "none",
            },
          },
        },
      },

      // --- Dialog / Drawer: same soft-shadow, 24px-radius card language,
      // with a smooth open/close transition (respecting reduced motion via
      // the global CssBaseline rule above, which already covers MUI's own
      // Modal/Slide transition durations).
      MuiDialog: {
        styleOverrides: {
          paper: {
            borderRadius: radiusPx.lg,
            backgroundImage: "none",
            boxShadow: isDark ? floatingShadowDark : namedShadows.floating,
          },
        },
      },
      MuiDialogTitle: {
        styleOverrides: {
          root: {
            fontSize: typographyScale.h5.fontSize,
            fontWeight: typographyScale.h5.fontWeight,
          },
        },
      },
      MuiDrawer: {
        styleOverrides: {
          paper: {
            backgroundImage: "none",
          },
        },
      },

      // --- Chip ("Badge"): fully pill-shaped per the brief. StatusPill's
      // "neutral" tone (`color="default"`, `variant="outlined"`) gets a
      // deliberate border/text colour instead of MUI's default action-grey,
      // so it reads as "no status opinion," not "disabled."
      MuiChip: {
        styleOverrides: {
          root: {
            borderRadius: radiusPx.pill,
            fontWeight: typographyScale.subtitle2.fontWeight,
          },
          outlined: {
            borderColor: isDark ? darkSurface.border : neutral[300],
            color: isDark ? darkNeutralPillText : neutral[700],
          },
        },
      },

      // --- Tooltip: an arrow on every tooltip app-wide via defaultProps.
      MuiTooltip: {
        defaultProps: {
          arrow: true,
        },
        styleOverrides: {
          tooltip: {
            backgroundColor: isDark ? darkSurface.elevated : neutral[800],
            color: isDark ? darkText.primary : undefined,
            fontSize: typographyScale.caption.fontSize,
            borderRadius: radiusPx.sm,
          },
          arrow: {
            color: isDark ? darkSurface.elevated : neutral[800],
          },
        },
      },

      // --- Navigation: Preply-style selected state — a soft, fully
      // rounded tinted background rather than Phase D1's left accent bar,
      // generous row height/spacing for "large clickable rows" (also
      // clears the 44px touch-target minimum).
      MuiListItemButton: {
        styleOverrides: {
          root: {
            borderRadius: radiusPx.md,
            minHeight: 48,
            paddingTop: 10,
            paddingBottom: 10,
            marginBottom: 4,
            transition: "background-color 150ms ease, color 150ms ease",
            "&.Mui-selected": {
              backgroundColor: `${focusRingColor}1F`,
              color: focusRingColor,
              "& .MuiListItemIcon-root": {
                color: focusRingColor,
              },
              "&:hover": {
                backgroundColor: `${focusRingColor}2E`,
              },
            },
            "&:focus-visible": {
              outline: `2px solid ${focusRingColor}`,
              outlineOffset: -2,
            },
            "@media (prefers-reduced-motion: reduce)": {
              transition: "none",
            },
          },
        },
      },
      MuiListItemIcon: {
        styleOverrides: {
          root: {
            minWidth: 40,
          },
        },
      },
    },
  });
}

/** The app's real, live theme — every existing import (`AppProviders.tsx`, every test file) keeps working unchanged. */
export const theme = createAppTheme("light");

/** Live in the real app since Phase D4 — `AppProviders.tsx` renders this whenever `ColorModeProvider`'s resolved mode is "dark" (`AppHeader`'s `ThemeToggle` switches it). */
export const darkTheme = createAppTheme("dark");
