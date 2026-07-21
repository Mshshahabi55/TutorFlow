import { createTheme } from "@mui/material/styles";

// A placeholder visual identity, not a ratified design decision — no
// approved document specifies branding, palette, or typography for
// TutorFlow. Chosen deliberately (a calm, professional slate/blue,
// consistent with "earn trust through reliability first") rather than left
// to MUI's own defaults, but this remains a Foundation-level implementation
// choice, revisitable the moment a real design system is supplied.
export const theme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: "#33607F",
      light: "#5C85A3",
      dark: "#20455E",
      contrastText: "#FFFFFF",
    },
    secondary: {
      main: "#8A5F10",
    },
    error: {
      main: "#8A3524",
    },
    warning: {
      main: "#8A5F10",
    },
    success: {
      main: "#2F6B43",
    },
    background: {
      default: "#F6F3EA",
      paper: "#FFFFFF",
    },
  },
  typography: {
    fontFamily: [
      "-apple-system",
      "Segoe UI",
      "Roboto",
      "Helvetica Neue",
      "Arial",
      "sans-serif",
    ].join(","),
    h1: { fontWeight: 700 },
    h2: { fontWeight: 700 },
    h3: { fontWeight: 700 },
    h4: { fontWeight: 700 },
    h5: { fontWeight: 600 },
    h6: { fontWeight: 600 },
  },
  shape: {
    borderRadius: 6,
  },
  components: {
    MuiButton: {
      defaultProps: {
        disableElevation: true,
      },
      styleOverrides: {
        root: {
          textTransform: "none",
        },
      },
    },
    MuiAppBar: {
      defaultProps: {
        elevation: 0,
      },
    },
  },
});
