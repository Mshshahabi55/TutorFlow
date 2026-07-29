import { useEffect, useState } from "react";

/** Re-renders the calling component every `intervalMs`, returning the current time — the only sane way to keep a countdown ticking without polling the server for it. */
export function useNow(intervalMs = 1000): Date {
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), intervalMs);
    return () => window.clearInterval(timer);
  }, [intervalMs]);

  return now;
}
