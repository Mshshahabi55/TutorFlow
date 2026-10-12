'use client';

import { createContext, useCallback, useContext, useState } from 'react';
import styles from './Toast.module.css';

interface ToastItem {
  id: number;
  message: string;
  variant: 'default' | 'success' | 'error';
}

interface ToastContextValue {
  showToast: (message: string, variant?: ToastItem['variant']) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

let nextId = 0;

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const showToast = useCallback((message: string, variant: ToastItem['variant'] = 'default') => {
    const id = nextId++;
    setToasts((prev) => [...prev, { id, message, variant }]);
    setTimeout(() => setToasts((prev) => prev.filter((t) => t.id !== id)), 5000);
  }, []);

  const dismiss = (id: number) => setToasts((prev) => prev.filter((t) => t.id !== id));

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      {/* A single polite live region for all toasts — screen readers announce each as it
          mounts without the page needing a separate alert per instance. */}
      <div className={styles.viewport} aria-live="polite" aria-atomic="true">
        {toasts.map((t) => (
          <div key={t.id} className={[styles.toast, styles[t.variant] ?? ''].join(' ')}>
            <span>{t.message}</span>
            <button className={styles.closeButton} onClick={() => dismiss(t.id)} aria-label="Dismiss notification">
              ✕
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    throw new Error('useToast must be used within a ToastProvider');
  }
  return ctx;
}
