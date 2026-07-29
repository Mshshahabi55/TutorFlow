import { useCallback, useState } from "react";

const STORAGE_KEY = "tutorflow.recentlyViewedTutorIds";
const MAX_ENTRIES = 10;

function readStoredIds(): string[] {
  if (typeof window === "undefined") {
    return [];
  }
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed) ? parsed.filter((id): id is string => typeof id === "string") : [];
  } catch {
    return [];
  }
}

function writeStoredIds(ids: string[]) {
  if (typeof window !== "undefined") {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(ids));
  }
}

/**
 * Client-side only, same reasoning as `useFavoriteTutors` — no backend
 * concept, no ADR needed, nothing sent over the network. Most-recent-first,
 * capped at MAX_ENTRIES (a bounded list, not an ever-growing log) with no
 * duplicates: re-viewing an already-recent Tutor moves it back to the front
 * rather than adding a second entry.
 */
export function useRecentlyViewedTutors() {
  const [recentIds, setRecentIds] = useState<string[]>(readStoredIds);

  const recordView = useCallback((tutorId: string) => {
    setRecentIds((previous) => {
      const next = [tutorId, ...previous.filter((id) => id !== tutorId)].slice(0, MAX_ENTRIES);
      writeStoredIds(next);
      return next;
    });
  }, []);

  return { recentIds, recordView };
}
