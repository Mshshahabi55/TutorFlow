import { useContext } from "react";
import {
  NotificationContext,
  type NotificationContextValue,
} from "@/shared/context/NotificationContext";

export function useNotification(): NotificationContextValue {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error("useNotification must be used within a NotificationProvider.");
  }

  return context;
}
