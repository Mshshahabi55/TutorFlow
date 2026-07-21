import { useState } from "react";

// Mirrors TutorFlow.Application.Common.PageRequest's own defaults exactly.
export const DEFAULT_PAGE = 1;
export const DEFAULT_PAGE_SIZE = 20;

export interface PaginationState {
  page: number;
  pageSize: number;
  setPage: (page: number) => void;
  setPageSize: (pageSize: number) => void;
  reset: () => void;
}

/** Local pagination state for a query-backed list, using the backend's own 1-based page convention. */
export function usePagination(initialPageSize: number = DEFAULT_PAGE_SIZE): PaginationState {
  const [page, setPage] = useState(DEFAULT_PAGE);
  const [pageSize, setPageSize] = useState(initialPageSize);

  return {
    page,
    pageSize,
    setPage,
    setPageSize,
    reset: () => {
      setPage(DEFAULT_PAGE);
      setPageSize(initialPageSize);
    },
  };
}
