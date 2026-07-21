import { createContext } from "react";

export type NotificationSeverity = "success" | "info" | "warning" | "error";

export interface NotifyOptions {
  message: string;
  severity?: NotificationSeverity;
  autoHideDurationMs?: number;
}

export interface NotificationContextValue {
  notify: (options: NotifyOptions) => void;
}

export const NotificationContext = createContext<NotificationContextValue | undefined>(
  undefined,
);
