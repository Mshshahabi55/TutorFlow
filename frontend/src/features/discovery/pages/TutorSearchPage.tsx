import { Stack, TablePagination, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useSearchTutors } from "@/features/discovery/hooks/useSearchTutors";
import {
  tutorSearchFiltersSchema,
  type TutorSearchFiltersFormValues,
} from "@/features/discovery/validation/tutorSearchFiltersSchema";
import type { SearchTutorsFilters } from "@/features/discovery/api/discoveryService";
import { SearchHero, type ActiveFilterChip } from "@/features/discovery/components/SearchHero";
import { TutorFilterPanel } from "@/features/discovery/components/TutorFilterPanel";
import { TutorCard } from "@/features/discovery/components/TutorCard";
import { TutorCardSkeleton } from "@/features/discovery/components/TutorCardSkeleton";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { PageHeader } from "@/shared/components/PageHeader";
import { usePagination } from "@/shared/hooks/usePagination";
import { fromTehranInput, toTehranDisplay } from "@/shared/time/tehranTime";

const EMPTY_FILTERS: SearchTutorsFilters = {
  subject: "",
  language: "",
  location: "",
  availableFrom: "",
};

const SKELETON_COUNT = 8;

/**
 * GET /tutors/search — Discovery's own capability, presented as a
 * marketplace directory instead of an internal search form. No filter, no
 * query parameter, and no pagination behavior changed from the prior
 * DataTable-based page (Phase 3 Step 2) — only how results are presented.
 */
export function TutorSearchPage() {
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

  function handleClearField(field: keyof SearchTutorsFilters) {
    form.setValue(field, "");
    setFilters((previous) => ({ ...previous, [field]: "" }));
    reset();
  }

  const activeFilters: ActiveFilterChip[] = [
    filters.subject
      ? {
          key: "subject",
          label: `Subject: ${filters.subject}`,
          onClear: () => handleClearField("subject"),
        }
      : null,
    filters.language
      ? {
          key: "language",
          label: `Language: ${filters.language}`,
          onClear: () => handleClearField("language"),
        }
      : null,
    filters.location
      ? {
          key: "location",
          label: `Location: ${filters.location}`,
          onClear: () => handleClearField("location"),
        }
      : null,
    filters.availableFrom
      ? {
          key: "availableFrom",
          label: `Available from: ${toTehranDisplay(filters.availableFrom)}`,
          onClear: () => handleClearField("availableFrom"),
        }
      : null,
  ].filter((chip): chip is ActiveFilterChip => chip !== null);

  const tutors = searchQuery.data?.items ?? [];

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Find your Tutor"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Search by subject, then narrow by language, location, or availability.
          </Typography>
        }
      />

      <Form form={form} onSubmit={handleSubmit}>
        <Stack spacing={2}>
          <SearchHero
            resultCount={searchQuery.data?.totalCount}
            isSearching={searchQuery.isPending}
            hasActiveFilters={hasActiveFilters}
            activeFilters={activeFilters}
            onClearAll={handleClear}
          />

          <TutorFilterPanel activeFilterCount={activeFilters.length}>
            <FormTextField name="language" label="Language" sx={{ minWidth: 160 }} />
            <FormTextField name="location" label="Location" sx={{ minWidth: 160 }} />
            <FormTextField
              name="availableFrom"
              label="Available from (Tehran)"
              type="datetime-local"
              slotProps={{ inputLabel: { shrink: true } }}
              sx={{ minWidth: 240 }}
            />
          </TutorFilterPanel>
        </Stack>
      </Form>

      {searchQuery.isPending ? (
        <Stack direction="row" flexWrap="wrap" gap={2}>
          {Array.from({ length: SKELETON_COUNT }, (_, index) => (
            <TutorCardSkeleton key={index} />
          ))}
        </Stack>
      ) : null}

      {searchQuery.isError ? (
        <ErrorState error={searchQuery.error} onRetry={() => void searchQuery.refetch()} />
      ) : null}

      {searchQuery.isSuccess && tutors.length === 0 ? (
        <EmptyState
          title="No Tutors match these filters"
          description={
            hasActiveFilters
              ? "Try broadening or clearing a filter above."
              : "Check back soon — no Tutors are registered yet."
          }
        />
      ) : null}

      {searchQuery.isSuccess && tutors.length > 0 ? (
        <>
          <Stack direction="row" flexWrap="wrap" gap={2}>
            {tutors.map((tutor) => (
              <TutorCard key={tutor.tutorId} tutor={tutor} />
            ))}
          </Stack>
          <TablePagination
            component="div"
            count={searchQuery.data?.totalCount ?? 0}
            page={page - 1}
            rowsPerPage={pageSize}
            rowsPerPageOptions={[10, 20, 50]}
            onPageChange={(_event, newPage) => setPage(newPage + 1)}
            onRowsPerPageChange={(event) => {
              setPageSize(Number(event.target.value));
              setPage(1);
            }}
          />
        </>
      ) : null}
    </Stack>
  );
}
