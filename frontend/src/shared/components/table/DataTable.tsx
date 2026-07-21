import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  Paper,
} from "@mui/material";
import type { ReactNode } from "react";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { EmptyState } from "@/shared/components/feedback/EmptyState";

export interface DataTableColumn<TRow> {
  key: string;
  header: string;
  render: (row: TRow) => ReactNode;
  align?: "left" | "right" | "center";
  width?: string | number;
}

/**
 * Server-side pagination state, using the backend's own 1-based page
 * convention (Application/Common/PageRequest.cs: DefaultPage = 1) — this
 * component translates to MUI TablePagination's 0-based convention
 * internally, so no consumer needs to.
 */
export interface DataTablePaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  pageSizeOptions?: number[];
}

export interface DataTableProps<TRow> {
  columns: DataTableColumn<TRow>[];
  rows: TRow[];
  getRowKey: (row: TRow) => string | number;
  isLoading?: boolean;
  error?: unknown;
  onRetry?: () => void;
  emptyState?: { title: string; description?: string };
  pagination?: DataTablePaginationProps;
  onRowClick?: (row: TRow) => void;
}

export function DataTable<TRow>({
  columns,
  rows,
  getRowKey,
  isLoading = false,
  error,
  onRetry,
  emptyState,
  pagination,
  onRowClick,
}: DataTableProps<TRow>) {
  if (isLoading) {
    return <LoadingState />;
  }

  if (error) {
    return <ErrorState error={error} onRetry={onRetry} />;
  }

  if (rows.length === 0) {
    return (
      <EmptyState
        title={emptyState?.title ?? "Nothing to show yet"}
        description={emptyState?.description}
      />
    );
  }

  return (
    <Paper variant="outlined">
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              {columns.map((column) => (
                <TableCell key={column.key} align={column.align} width={column.width} scope="col">
                  {column.header}
                </TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <TableRow
                key={getRowKey(row)}
                hover={Boolean(onRowClick)}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                tabIndex={onRowClick ? 0 : undefined}
                onKeyDown={
                  onRowClick
                    ? (event) => {
                        if (event.key === "Enter" || event.key === " ") {
                          event.preventDefault();
                          onRowClick(row);
                        }
                      }
                    : undefined
                }
                sx={
                  onRowClick
                    ? {
                        cursor: "pointer",
                        "&:focus-visible": {
                          outline: "2px solid",
                          outlineColor: "primary.main",
                          outlineOffset: -2,
                        },
                      }
                    : undefined
                }
              >
                {columns.map((column) => (
                  <TableCell key={column.key} align={column.align}>
                    {column.render(row)}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      {pagination ? (
        <TablePagination
          component="div"
          count={pagination.totalCount}
          page={pagination.page - 1}
          rowsPerPage={pagination.pageSize}
          rowsPerPageOptions={pagination.pageSizeOptions ?? [10, 20, 50]}
          onPageChange={(_event, newPage) => pagination.onPageChange(newPage + 1)}
          onRowsPerPageChange={(event) => {
            pagination.onPageSizeChange(Number(event.target.value));
            pagination.onPageChange(1);
          }}
        />
      ) : null}
    </Paper>
  );
}
