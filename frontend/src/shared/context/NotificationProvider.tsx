import { Alert, Snackbar } from "@mui/material";
import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import {
  NotificationContext,
  type NotificationContextValue,
  type NotificationSeverity,
  type NotifyOptions,
} from "@/shared/context/NotificationContext";

interface QueuedNotification {
  key: number;
  message: string;
  severity: NotificationSeverity;
  autoHideDurationMs: number;
}

const DEFAULT_AUTO_HIDE_MS = 5000;

/** A single-at-a-time snackbar queue — simple, predictable, no overlapping toasts. */
export function NotificationProvider({ children }: { children: ReactNode }) {
  const [queue, setQueue] = useState<QueuedNotification[]>([]);
  const [current, setCurrent] = useState<QueuedNotification | null>(null);
  const [open, setOpen] = useState(false);
  const nextKey = useRef(0);

  const notify = useCallback((options: NotifyOptions) => {
    nextKey.current += 1;
    setQueue((prev) => [
      ...prev,
      {
        key: nextKey.current,
        message: options.message,
        severity: options.severity ?? "info",
        autoHideDurationMs: options.autoHideDurationMs ?? DEFAULT_AUTO_HIDE_MS,
      },
    ]);
  }, []);

  useEffect(() => {
    if (!current && queue.length > 0) {
      setCurrent(queue[0]);
      setQueue((prev) => prev.slice(1));
      setOpen(true);
    }
  }, [current, queue]);

  const handleClose = (_event: unknown, reason?: string) => {
    if (reason === "clickaway") {
      return;
    }
    setOpen(false);
  };

  const handleExited = () => {
    setCurrent(null);
  };

  const value = useMemo<NotificationContextValue>(() => ({ notify }), [notify]);

  return (
    <NotificationContext.Provider value={value}>
      {children}
      <Snackbar
        key={current?.key}
        open={open}
        autoHideDuration={current?.autoHideDurationMs}
        onClose={handleClose}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
        TransitionProps={{ onExited: handleExited }}
      >
        {current ? (
          <Alert onClose={handleClose} severity={current.severity} variant="filled" sx={{ width: "100%" }}>
            {current.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </NotificationContext.Provider>
  );
}
