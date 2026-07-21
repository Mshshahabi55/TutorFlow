import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { DataTable, type DataTableColumn } from "@/shared/components/table/DataTable";

interface Row {
  id: string;
  name: string;
}

const columns: DataTableColumn<Row>[] = [
  { key: "name", header: "Name", render: (row) => row.name },
];

describe("DataTable", () => {
  it("renders a loading state", () => {
    render(<DataTable columns={columns} rows={[]} getRowKey={(r) => r.id} isLoading />);

    expect(screen.getByRole("status")).toBeInTheDocument();
  });

  it("renders an error state and forwards retry", async () => {
    const onRetry = vi.fn();
    render(
      <DataTable columns={columns} rows={[]} getRowKey={(r) => r.id} error={new Error("boom")} onRetry={onRetry} />,
    );

    await userEvent.click(screen.getByRole("button", { name: /try again/i }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("renders an empty state when there are no rows", () => {
    render(
      <DataTable
        columns={columns}
        rows={[]}
        getRowKey={(r) => r.id}
        emptyState={{ title: "No Tutors yet" }}
      />,
    );

    expect(screen.getByText("No Tutors yet")).toBeInTheDocument();
  });

  it("renders a row per item using each column's render function", () => {
    const rows: Row[] = [
      { id: "1", name: "Ada" },
      { id: "2", name: "Grace" },
    ];

    render(<DataTable columns={columns} rows={rows} getRowKey={(r) => r.id} />);

    expect(screen.getByText("Ada")).toBeInTheDocument();
    expect(screen.getByText("Grace")).toBeInTheDocument();
  });

  it("invokes onRowClick with the clicked row", async () => {
    const rows: Row[] = [{ id: "1", name: "Ada" }];
    const onRowClick = vi.fn();

    render(<DataTable columns={columns} rows={rows} getRowKey={(r) => r.id} onRowClick={onRowClick} />);

    await userEvent.click(screen.getByText("Ada"));

    expect(onRowClick).toHaveBeenCalledWith(rows[0]);
  });

  it("makes clickable rows keyboard-focusable and activates onRowClick on Enter", async () => {
    const rows: Row[] = [{ id: "1", name: "Ada" }];
    const onRowClick = vi.fn();

    render(<DataTable columns={columns} rows={rows} getRowKey={(r) => r.id} onRowClick={onRowClick} />);

    await userEvent.tab();
    expect(screen.getByText("Ada").closest("tr")).toHaveFocus();

    await userEvent.keyboard("{Enter}");
    expect(onRowClick).toHaveBeenCalledWith(rows[0]);
  });

  it("activates onRowClick on Space without scrolling the page", async () => {
    const rows: Row[] = [{ id: "1", name: "Ada" }];
    const onRowClick = vi.fn();

    render(<DataTable columns={columns} rows={rows} getRowKey={(r) => r.id} onRowClick={onRowClick} />);

    await userEvent.tab();
    await userEvent.keyboard(" ");

    expect(onRowClick).toHaveBeenCalledWith(rows[0]);
  });

  it("does not make rows focusable when onRowClick is not provided", () => {
    const rows: Row[] = [{ id: "1", name: "Ada" }];

    render(<DataTable columns={columns} rows={rows} getRowKey={(r) => r.id} />);

    expect(screen.getByText("Ada").closest("tr")).not.toHaveAttribute("tabindex");
  });

  it("translates MUI's 0-based page change into the backend's 1-based convention", async () => {
    const rows: Row[] = Array.from({ length: 5 }, (_, i) => ({ id: String(i), name: `Row ${i}` }));
    const onPageChange = vi.fn();

    render(
      <DataTable
        columns={columns}
        rows={rows}
        getRowKey={(r) => r.id}
        pagination={{
          page: 1,
          pageSize: 10,
          totalCount: 12,
          onPageChange,
          onPageSizeChange: vi.fn(),
        }}
      />,
    );

    await userEvent.click(screen.getByRole("button", { name: /next page/i }));

    expect(onPageChange).toHaveBeenCalledWith(2);
  });
});
