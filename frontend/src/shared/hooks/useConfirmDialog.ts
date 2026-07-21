import { useContext } from "react";
import {
  ConfirmDialogContext,
  type ConfirmDialogContextValue,
} from "@/shared/context/ConfirmDialogContext";

export function useConfirmDialog(): ConfirmDialogContextValue {
  const context = useContext(ConfirmDialogContext);
  if (!context) {
    throw new Error("useConfirmDialog must be used within a ConfirmDialogProvider.");
  }

  return context;
}
