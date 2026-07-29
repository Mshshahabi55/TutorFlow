import { useCallback, useState } from "react";
import type { SearchTutorsFilters } from "@/features/discovery/api/discoveryService";

const STORAGE_KEY = "tutorflow.savedSearches";
const MAX_ENTRIES = 20;

export interface SavedSearch {
  id: string;
  name: string;
  filters: SearchTutorsFilters;
  savedAtUtc: string;
}

function isSearchTutorsFilters(value: unknown): value is SearchTutorsFilters {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  const record = value as Record<string, unknown>;
  return (
    typeof record.subject === "string" &&
    typeof record.language === "string" &&
    typeof record.location === "string" &&
    typeof record.availableFrom === "string"
  );
}

function isSavedSearch(value: unknown): value is SavedSearch {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  const record = value as Record<string, unknown>;
  return (
    typeof record.id === "string" &&
    typeof record.name === "string" &&
    typeof record.savedAtUtc === "string" &&
    isSearchTutorsFilters(record.filters)
  );
}

function readStored(): SavedSearch[] {
  if (typeof window === "undefined") {
    return [];
  }
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed) ? parsed.filter(isSavedSearch) : [];
  } catch {
    return [];
  }
}

function writeStored(searches: SavedSearch[]) {
  if (typeof window !== "undefined") {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(searches));
  }
}

/**
 * Client-side only (same reasoning as `useFavoriteTutors`) — a named
 * snapshot of `SearchTutorsFilters` a Student can re-apply with one click.
 * Capped at MAX_ENTRIES; saving beyond that silently drops the oldest
 * entry rather than growing forever or blocking the save.
 */
export function useSavedSearches() {
  const [savedSearches, setSavedSearches] = useState<SavedSearch[]>(readStored);

  const saveSearch = useCallback((name: string, filters: SearchTutorsFilters) => {
    setSavedSearches((previous) => {
      const entry: SavedSearch = {
        id: typeof crypto !== "undefined" && "randomUUID" in crypto ? crypto.randomUUID() : `${Date.now()}`,
        name,
        filters,
        savedAtUtc: new Date().toISOString(),
      };
      const next = [entry, ...previous].slice(0, MAX_ENTRIES);
      writeStored(next);
      return next;
    });
  }, []);

  const removeSearch = useCallback((id: string) => {
    setSavedSearches((previous) => {
      const next = previous.filter((search) => search.id !== id);
      writeStored(next);
      return next;
    });
  }, []);

  return { savedSearches, saveSearch, removeSearch };
}
