import { createTheme } from "@mui/material/styles";
import type { Shadows } from "@mui/material/styles";

/**
 * TutorFlow's design tokens (Phase D1 — Design System Foundation).
 *
 * This is the single source of every colour, type size, spacing value,
 * radius, shadow, breakpoint, and z-index used in the UI. No component
 * outside this file should hardcode a hex value, a pixel size, or a
 * spacing number — it should reach for a theme token instead (a palette
 * key, a typography variant, a spacing multiple, `shape.borderRadius`).
 *
 * Every named export below (`neutral`, `typographyScale`, `spacingUnitPx`,
 * `radiusPx`, `namedShadows`) is the same value the theme is built from —
 * exported separately so the dev-only style guide page (Task 4) can render
 * an accurate swatch of the raw tokens without reverse-engineering them out
 * of a constructed MUI theme object.
 *
 * Still a Foundation-level implementation choice, not a ratified brand —
 * no approved document specifies branding for TutorFlow. Chosen
 * deliberately (calm, low-saturation, cool neutral — the Linear/Notion/
 * Stripe Dashboard reference class named in the Phase D1 brief) rather
 * than left to MUI's own defaults, and revisitable the moment a real
 * brand is supplied.
 */

// ---------------------------------------------------------------------------
// Colour — a neutral grey scale as the workhorse, one restrained accent
// (primary), and four semantic tones (success/warning/error/info). Every
// tone below was chosen to clear WCAG AA (4.5:1) both as text-on-white and
// as white-text-on-fill (a Chip/Button use the same hex both ways) — see
// docs/design/DESIGN-SYSTEM.md for the measured ratio of every pair.
// ---------------------------------------------------------------------------

/** Cool neutral grey scale. 50 = lightest surface tint, 900 = darkest text. */
const neutral = {
  50: "#F8F9FA",
  100: "#EEF1F3",
  200: "#E1E5E9",
  300: "#CBD2D9",
  400: "#9AA5B1",
  500: "#6B7684",
  600: "#4C5563",
  700: "#363E4A",
  800: "#232933",
  900: "#14181F",
} as const;

const accent = {
  main: "#2E6486",
  light: "#5C85A3",
  dark: "#1F4258",
  contrastText: "#FFFFFF",
};

const semantic = {
  success: { main: "#1E7A42", contrastText: "#FFFFFF" },
  warning: { main: "#8A5F10", contrastText: "#FFFFFF" },
  error: { main: "#8A3524", contrastText: "#FFFFFF" },
  info: { main: "#1565A6", contrastText: "#FFFFFF" },
};

// ---------------------------------------------------------------------------
// Typography — one family (the system stack: no self-hosted webfont, see
// DESIGN-SYSTEM.md for why), a modular scale (~1.2–1.25 ratio) mapped onto
// MUI's own variant names. Those names (h1..h6, subtitle1/2, body1/2,
// caption, overline, button) are already semantic, not pixel-named — every
// call site in the app already writes `variant="h4"` or `variant="body2"`,
// never a raw font-size — so this phase keeps that convention rather than
// inventing a parallel custom scale.
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
  h1: { fontSize: "3rem", lineHeight: 1.2, fontWeight: 700 }, // 48px — unused today, reserved
  h2: { fontSize: "2.25rem", lineHeight: 1.25, fontWeight: 700 }, // 36px — unused today, reserved
  h3: { fontSize: "1.875rem", lineHeight: 1.3, fontWeight: 700 }, // 30px — unused today, reserved
  h4: { fontSize: "1.5rem", lineHeight: 1.35, fontWeight: 700 }, // 24px — PageHeader title
  h5: { fontSize: "1.25rem", lineHeight: 1.4, fontWeight: 600 }, // 20px
  h6: { fontSize: "1.125rem", lineHeight: 1.4, fontWeight: 600 }, // 18px — AppBar title
  subtitle1: { fontSize: "1rem", lineHeight: 1.5, fontWeight: 600 }, // 16px — card/section headings
  subtitle2: { fontSize: "0.875rem", lineHeight: 1.5, fontWeight: 600 }, // 14px
  body1: { fontSize: "1rem", lineHeight: 1.5, fontWeight: 400 }, // 16px — default body copy
  body2: { fontSize: "0.875rem", lineHeight: 1.57, fontWeight: 400 }, // 14px — secondary/dense copy
  caption: { fontSize: "0.75rem", lineHeight: 1.5, fontWeight: 400 }, // 12px — unit suffixes, helper text
  overline: {
    fontSize: "0.75rem",
    lineHeight: 1.5,
    fontWeight: 600,
    letterSpacing: "0.06em",
    textTransform: "uppercase" as const,
  },
  button: { fontSize: "0.875rem", lineHeight: 1.5, fontWeight: 600, textTransform: "none" as const },
} as const;

// ---------------------------------------------------------------------------
// Spacing — MUI's own `theme.spacing(n)` multiplier. Base unit 8px, used via
// `spacing`/`gap`/`p`/`m` props and the `Stack`'s `spacing` prop everywhere
// in the app already (the Task 1 audit found no inline pixel spacing outside
// this file) — so the token here is a statement of the existing convention,
// not a migration.
// ---------------------------------------------------------------------------
export const spacingUnitPx = 8;

// ---------------------------------------------------------------------------
// Shape — one small radius set. Card/Paper/Dialog/TextField all read
// `shape.borderRadius`; Chip (StatusPill) stays fully pill-shaped, which is
// its own visual language for "status", not a shared radius token.
// ---------------------------------------------------------------------------
export const radiusPx = {
  sm: 4, // dense inline elements (Chip's own radius is pill, not this)
  md: 8, // default: Button, TextField, Card, Paper, Dialog
} as const;

// ---------------------------------------------------------------------------
// Shadow — depth comes from a 1px border (`variant="outlined"`, already the
// convention on every Card/Paper in the app) and from spacing, not drop
// shadow, per the Phase D1 brief ("no gradients, no decorative motion").
// MUI's `shadows` array must have exactly 25 entries (elevation 0–24); this
// flattens Material's default heavy shadow ramp down to two deliberately
// subtle tiers instead of removing shadow capability outright, since a small
// number of MUI components (Menu, Popover, Dialog, Snackbar) still rely on
// elevation to separate themselves from page content when they float above
// it with no bordered container of their own.
// ---------------------------------------------------------------------------
const restingShadow = "0px 1px 2px rgba(20, 24, 31, 0.06), 0px 1px 1px rgba(20, 24, 31, 0.04)";
const floatingShadow = "0px 4px 12px rgba(20, 24, 31, 0.10), 0px 2px 4px rgba(20, 24, 31, 0.06)";

export const namedShadows = { none: "none", resting: restingShadow, floating: floatingShadow } as const;

const shadows = Array.from({ length: 25 }, (_, elevation) => {
  if (elevation === 0) return namedShadows.none;
  if (elevation <= 6) return namedShadows.resting;
  return namedShadows.floating;
}) as unknown as Shadows;

// ---------------------------------------------------------------------------
// Breakpoints & z-index — MUI's own defaults, named rather than magic
// numbers, kept as-is: AppLayout already switches its nav drawer at `md`
// (permanent from tablet-landscape/desktop up) and stacks form rows at `sm`
// (Task 1 audit); the app has no need that isn't already served by
// xs/sm/md/lg/xl and MUI's standard z-index ladder
// (mobileStepper/appBar/drawer/modal/snackbar/tooltip).
// ---------------------------------------------------------------------------

/**
 * The monospace stack every raw-id display (`CopyableId`, and the id
 * subtitle a few detail pages set inline) should read from, instead of each
 * call site repeating its own `"ui-monospace, monospace"` literal — a single
 * token for the one typographic idea "this is an identifier, not prose".
 */
export const monoFontFamily = "ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";

export const theme = createTheme({
  palette: {
    mode: "light",
    primary: accent,
    secondary: { main: neutral[700], contrastText: "#FFFFFF" },
    success: semantic.success,
    warning: semantic.warning,
    error: semantic.error,
    info: semantic.info,
    grey: neutral,
    text: {
      primary: neutral[900],
      secondary: neutral[600],
      disabled: neutral[400],
    },
    background: {
      default: neutral[50],
      paper: "#FFFFFF",
    },
    divider: neutral[200],
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
  shadows,
  components: {
    // --- Button: primary=contained, secondary action=outlined, low-emphasis
    // =text, destructive=color="error" (outlined to propose, contained inside
    // the confirm dialog) — an existing convention (Task 1 audit), not new.
    // This only adds a visible, non-color focus ring and a firmer disabled
    // state; call sites are untouched.
    MuiButton: {
      defaultProps: {
        disableElevation: true,
      },
      styleOverrides: {
        root: {
          textTransform: "none",
          "&:focus-visible": {
            outline: `2px solid ${accent.main}`,
            outlineOffset: 2,
          },
        },
      },
    },
    MuiIconButton: {
      styleOverrides: {
        root: {
          "&:focus-visible": {
            outline: `2px solid ${accent.main}`,
            outlineOffset: 2,
          },
        },
      },
    },
    MuiAppBar: {
      defaultProps: {
        elevation: 0,
      },
    },

    // --- Form controls: one label/helper/error treatment for every text
    // field, select, checkbox, radio — including the datetime-local inputs
    // used for every Tehran-time entry point, which are plain MuiTextField
    // instances and so inherit this automatically.
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          borderRadius: radiusPx.md,
          backgroundColor: "#FFFFFF",
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
            outline: `2px solid ${accent.main}`,
            outlineOffset: 2,
          },
        },
      },
    },
    MuiRadio: {
      styleOverrides: {
        root: {
          "&:focus-visible": {
            outline: `2px solid ${accent.main}`,
            outlineOffset: 2,
          },
        },
      },
    },

    // --- Card / Paper: depth from a 1px divider-colour border
    // (variant="outlined", already the convention everywhere), not shadow.
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: "none",
        },
        outlined: {
          borderColor: neutral[200],
        },
      },
    },
    MuiCard: {
      defaultProps: {
        variant: "outlined",
      },
    },

    // --- DataTable's Table/TableCell: a clearly distinct header row,
    // consistent cell padding, and a divider-colour bottom border instead of
    // MUI's default per-row border everywhere.
    MuiTableHead: {
      styleOverrides: {
        root: {
          backgroundColor: neutral[50],
          "& .MuiTableCell-root": {
            fontWeight: typographyScale.subtitle2.fontWeight,
            color: neutral[700],
            borderBottomColor: neutral[200],
          },
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: {
          borderBottomColor: neutral[200],
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

    // --- Dialog: same outlined-card language, no heavy shadow.
    MuiDialog: {
      styleOverrides: {
        paper: {
          backgroundImage: "none",
          boxShadow: namedShadows.floating,
        },
      },
    },
    MuiDialogTitle: {
      styleOverrides: {
        root: {
          fontSize: typographyScale.h6.fontSize,
          fontWeight: typographyScale.h6.fontWeight,
        },
      },
    },

    // --- StatusPill (Chip): the "neutral" tone renders as an outlined chip
    // (StatusPill.tsx `color="default"`) — give it a deliberate border/text
    // colour instead of MUI's default action-grey, so it reads as "no
    // status opinion" rather than "disabled".
    MuiChip: {
      styleOverrides: {
        outlined: {
          borderColor: neutral[300],
          color: neutral[700],
        },
      },
    },

    // --- Tooltip: an arrow on every tooltip app-wide (CopyableId's copy
    // button, RoleSwitcher's clear button, ...) via defaultProps, so each
    // call site does not have to opt in individually.
    MuiTooltip: {
      defaultProps: {
        arrow: true,
      },
      styleOverrides: {
        tooltip: {
          backgroundColor: neutral[800],
          fontSize: typographyScale.caption.fontSize,
          borderRadius: radiusPx.sm,
        },
        arrow: {
          color: neutral[800],
        },
      },
    },

    // --- Navigation: the selected route's ListItemButton (NavSidebar) gets
    // an accent-tinted background and a left accent bar — the one piece of
    // "where am I" orientation the pre-D1 sidebar had no visual answer for
    // at all (Task 1 audit).
    MuiListItemButton: {
      styleOverrides: {
        root: {
          borderRadius: radiusPx.sm,
          "&.Mui-selected": {
            backgroundColor: `${accent.main}1A`,
            borderLeft: `3px solid ${accent.main}`,
            paddingLeft: 13, // 16px default minus the 3px border, so text doesn't shift
            "&:hover": {
              backgroundColor: `${accent.main}26`,
            },
          },
          "&:focus-visible": {
            outline: `2px solid ${accent.main}`,
            outlineOffset: -2,
          },
        },
      },
    },
  },
});
