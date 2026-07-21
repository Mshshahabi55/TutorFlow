import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from "@mui/material";
import { useCallback, useMemo, useRef, useState, type ReactNode } from "react";
import {
  ConfirmDialogContext,
  type ConfirmDialogContextValue,
  type ConfirmOptions,
} from "@/shared/context/ConfirmDialogContext";

/** A single, imperative confirm dialog shared app-wide — avoids every consumer re-implementing its own confirm modal and local open/close state. */
export function ConfirmDialogProvider({ children }: { children: ReactNode }) {
  const [options, setOptions] = useState<ConfirmOptions | null>(null);
  const resolveRef = useRef<((confirmed: boolean) => void) | null>(null);

  const confirm = useCallback((nextOptions: ConfirmOptions) => {
    setOptions(nextOptions);
    return new Promise<boolean>((resolve) => {
      resolveRef.current = resolve;
    });
  }, []);

  const settle = (confirmed: boolean) => {
    resolveRef.current?.(confirmed);
    resolveRef.current = null;
    setOptions(null);
  };

  const value = useMemo<ConfirmDialogContextValue>(() => ({ confirm }), [confirm]);

  return (
    <ConfirmDialogContext.Provider value={value}>
      {children}
      <Dialog open={options !== null} onClose={() => settle(false)} maxWidth="xs" fullWidth>
        <DialogTitle>{options?.title}</DialogTitle>
        {options?.description ? (
          <DialogContent>
            <DialogContentText>{options.description}</DialogContentText>
          </DialogContent>
        ) : null}
        <DialogActions>
          <Button onClick={() => settle(false)} color="inherit">
            {options?.cancelLabel ?? "Cancel"}
          </Button>
          <Button
            onClick={() => settle(true)}
            color={options?.destructive ? "error" : "primary"}
            variant="contained"
            autoFocus
          >
            {options?.confirmLabel ?? "Confirm"}
          </Button>
        </DialogActions>
      </Dialog>
    </ConfirmDialogContext.Provider>
  );
}
