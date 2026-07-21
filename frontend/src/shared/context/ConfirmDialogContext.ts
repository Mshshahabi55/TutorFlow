import { createContext } from "react";

export interface ConfirmOptions {
  title: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
}

export interface ConfirmDialogContextValue {
  /** Resolves true if confirmed, false if cancelled/dismissed. */
  confirm: (options: ConfirmOptions) => Promise<boolean>;
}

export const ConfirmDialogContext = createContext<ConfirmDialogContextValue | undefined>(
  undefined,
);
