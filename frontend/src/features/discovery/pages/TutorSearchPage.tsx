import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useSearchTutors } from "@/features/discovery/hooks/useSearchTutors";
import {
  tutorSearchFiltersSchema,
  type TutorSearchFiltersFormValues,
} from "@/features/discovery/validation/tutorSearchFiltersSchema";
import type { SearchTutorsFilters } from "@/features/discovery/api/discoveryService";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { DataTable, type DataTableColumn } from "@/shared/components/table/DataTable";
import { PageHeader } from "@/shared/components/PageHeader";
import { usePagination } from "@/shared/hooks/usePagination";
import { formatMinutesList } from "@/shared/utils/duration";
import { fromTehranInput } from "@/shared/time/tehranTime";
import { paths } from "@/routes/paths";
import type { TutorDto } from "@/services/api/dtos";

const EMPTY_FILTERS: SearchTutorsFilters = {
  subject: "",
  language: "",
  location: "",
  availableFrom: "",
};

const columns: DataTableColumn<TutorDto>[] = [
  { key: "subject", header: "Subject", render: (row) => row.subject ?? "—" },
  { key: "language", header: "Language", render: (row) => row.language ?? "—" },
  { key: "location", header: "Location", render: (row) => row.location ?? "—" },
  {
    key: "hourlyRate",
    header: "Hourly rate",
    align: "right",
    render: (row) => (row.hourlyRate !== null ? row.hourlyRate : "—"),
  },
  {
    key: "offeredDurations",
    header: "Durations (min)",
    render: (row) =>
      row.offeredDurations.length > 0 ? formatMinutesList(row.offeredDurations) : "—",
  },
];

/**
 * GET /tutors/search — Discovery's own capability, composed here as the
 * entry point into the Booking flow: find a Tutor, then look up their
 * Availability Slot by id (no slot-browsing capability exists — see the
 * Sprint 7 Completion Report) or declare/view a session against them.
 */
export function TutorSearchPage() {
  const navigate = useNavigate();
  const [filters, setFilters] = useState<SearchTutorsFilters>(EMPTY_FILTERS);
  const { page, pageSize, setPage, setPageSize, reset } = usePagination();
  const searchQuery = useSearchTutors(filters, page, pageSize);

  const form = useForm<TutorSearchFiltersFormValues>({
    resolver: zodResolver(tutorSearchFiltersSchema),
    defaultValues: EMPTY_FILTERS,
  });

  const hasActiveFilters = Object.values(filters).some((value) => value !== "");

  function handleSubmit(values: TutorSearchFiltersFormValues) {
    setFilters({
      ...values,
      availableFrom: values.availableFrom === "" ? "" : fromTehranInput(values.availableFrom),
    });
    reset();
  }

  function handleClear() {
    form.reset(EMPTY_FILTERS);
    setFilters(EMPTY_FILTERS);
    reset();
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Search Tutors"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Every filter is optional — leave a field blank to not filter by it.
          </Typography>
        }
      />

      <Form form={form} onSubmit={handleSubmit}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} flexWrap="wrap" useFlexGap>
          <FormTextField name="subject" label="Subject" sx={{ minWidth: 160 }} />
          <FormTextField name="language" label="Language" sx={{ minWidth: 160 }} />
          <FormTextField name="location" label="Location" sx={{ minWidth: 160 }} />
          <FormTextField
            name="availableFrom"
            label="Available from (Tehran)"
            type="datetime-local"
            slotProps={{ inputLabel: { shrink: true } }}
            sx={{ minWidth: 240 }}
          />
          <Stack direction="row" spacing={1} alignSelf={{ sm: "flex-start" }}>
            <Button type="submit" variant="contained">
              Search
            </Button>
            {hasActiveFilters ? (
              <Button variant="text" onClick={handleClear}>
                Clear filters
              </Button>
            ) : null}
          </Stack>
        </Stack>
      </Form>

      <DataTable
        columns={columns}
        rows={searchQuery.data?.items ?? []}
        getRowKey={(row) => row.tutorId}
        isLoading={searchQuery.isPending}
        error={searchQuery.isError ? searchQuery.error : undefined}
        onRetry={() => void searchQuery.refetch()}
        emptyState={{
          title: "No Tutors match these filters",
          description: "Try broadening or clearing a filter.",
        }}
        pagination={{
          page,
          pageSize,
          totalCount: searchQuery.data?.totalCount ?? 0,
          onPageChange: setPage,
          onPageSizeChange: setPageSize,
        }}
        onRowClick={(row) => {
          void navigate(paths.identity.tutorDetail(row.tutorId));
        }}
      />
    </Stack>
  );
}
