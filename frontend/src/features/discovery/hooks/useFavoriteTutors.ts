import { useCallback, useState } from "react";

const STORAGE_KEY = "tutorflow.favoriteTutorIds";

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
 * Presentation-only, client-side "Favorite a Tutor" — no `Favorite`
 * aggregate exists in `DOMAIN_MODEL.md` and none is authorized by any
 * Accepted ADR, so this deliberately never calls a backend endpoint,
 * following the same pattern already established for the Learning Plans
 * stub (docs/adr/ADR-021, Proposed). Persisted per-browser via
 * `localStorage` (the `tutorflow.*` key convention every other client-only
 * preference in this app already uses — `useRememberedId`,
 * `tutorflow.sidebarCollapsed`), not per-Account, and lost if the Student
 * switches devices or clears storage. A server-side version needs its own
 * ADR before it can be built (see the decision document this ships
 * alongside).
 */
export function useFavoriteTutors() {
  const [favoriteIds, setFavoriteIds] = useState<string[]>(readStoredIds);

  const isFavorite = useCallback((tutorId: string) => favoriteIds.includes(tutorId), [favoriteIds]);

  const toggleFavorite = useCallback((tutorId: string) => {
    setFavoriteIds((previous) => {
      const next = previous.includes(tutorId)
        ? previous.filter((id) => id !== tutorId)
        : [...previous, tutorId];
      writeStoredIds(next);
      return next;
    });
  }, []);

  return { favoriteIds, isFavorite, toggleFavorite };
}
