import { Box, Button, Divider, Stack, TablePagination, Typography } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useSearchTutors } from "@/features/discovery/hooks/useSearchTutors";
import { useTutorsByIds } from "@/features/identity/hooks/useTutorQueries";
import {
  tutorSearchFiltersSchema,
  type TutorSearchFiltersFormValues,
} from "@/features/discovery/validation/tutorSearchFiltersSchema";
import type { SearchTutorsFilters } from "@/features/discovery/api/discoveryService";
import { SearchHero } from "@/features/discovery/components/SearchHero";
import { ActiveFiltersBar, type ActiveFilterChip } from "@/features/discovery/components/ActiveFiltersBar";
import { SearchResultsHeader } from "@/features/discovery/components/SearchResultsHeader";
import { TutorFilterPanel } from "@/features/discovery/components/TutorFilterPanel";
import { TutorCard } from "@/features/discovery/components/TutorCard";
import { TutorCardSkeleton } from "@/features/discovery/components/TutorCardSkeleton";
import { RecentlyViewedSection } from "@/features/discovery/components/RecentlyViewedSection";
import { SavedSearchesPanel } from "@/features/discovery/components/SavedSearchesPanel";
import { CompareBar, MAX_COMPARE_COUNT } from "@/features/discovery/components/CompareBar";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { UnavailableState } from "@/shared/components/feedback/UnavailableState";
import { PageHeader } from "@/shared/components/PageHeader";
import { paths } from "@/routes/paths";
import { usePagination } from "@/shared/hooks/usePagination";
import { fromTehranInput, toTehranDisplay, toTehranInputValue } from "@/shared/time/tehranTime";

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
  const [compareIds, setCompareIds] = useState<string[]>([]);
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

  /** Re-applies a saved search: the raw `SearchTutorsFilters` (already in wire format, availableFrom already a UTC ISO string) drives both the query and the form fields shown in the filter panel — the form's `datetime-local` input needs the Tehran-local input value, not the wire one, for that one field (same conversion `toFormValues`-style helpers elsewhere in this app use). */
  function handleApplySavedSearch(saved: SearchTutorsFilters) {
    form.reset({
      ...saved,
      availableFrom: saved.availableFrom === "" ? "" : toTehranInputValue(saved.availableFrom),
    });
    setFilters(saved);
    reset();
  }

  function toggleCompare(tutorId: string) {
    setCompareIds((previous) =>
      previous.includes(tutorId) ? previous.filter((id) => id !== tutorId) : [...previous, tutorId].slice(0, MAX_COMPARE_COUNT),
    );
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

  // Fetched independently from the current results page (same order as
  // compareIds, per useQueries) — a Tutor selected for comparison before
  // paginating away must still show up in CompareBar.
  const compareTutorQueries = useTutorsByIds(compareIds);
  const compareTutors = compareTutorQueries
    .map((query) => (query.isSuccess ? query.data : null))
    .filter((tutor): tutor is NonNullable<typeof tutor> => tutor !== null);

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
        <Stack spacing={2.5}>
          {/*
           * Search + Filters-trigger stay sticky just below the fixed
           * AppHeader while scrolling through results (top:88 matches the
           * offset TutorDetailPage's own sticky booking rail already uses)
           * — this is the "sticky search / sticky filter button" mobile
           * requirement, applied at every viewport rather than only on
           * mobile, since it's harmless on desktop too.
           */}
          <Box
            sx={{
              position: "sticky",
              top: 88,
              zIndex: 2,
              bgcolor: "background.default",
              pt: 0.5,
              pb: 1.5,
            }}
          >
            <Stack spacing={1.5}>
              <SearchHero />
              <Box>
                <TutorFilterPanel activeFilterCount={activeFilters.length}>
                  <Box>
                    <Typography variant="overline" color="text.secondary">
                      Where &amp; language
                    </Typography>
                    <Stack spacing={2} mt={1}>
                      <FormTextField name="language" label="Language" />
                      <FormTextField name="location" label="Location" />
                    </Stack>
                  </Box>
                  <Divider />
                  <Box>
                    <Typography variant="overline" color="text.secondary">
                      Availability
                    </Typography>
                    <Stack spacing={2} mt={1}>
                      <FormTextField
                        name="availableFrom"
                        label="Available from (Tehran)"
                        type="datetime-local"
                        slotProps={{ inputLabel: { shrink: true } }}
                      />
                    </Stack>
                  </Box>
                </TutorFilterPanel>
              </Box>
            </Stack>
          </Box>

          <Typography variant="caption" color="text.secondary">
            Tip: leave a filter blank to widen your results — subject, language, location, and
            availability all combine together.
          </Typography>

          {/* Active filters step */}
          <ActiveFiltersBar activeFilters={activeFilters} onClearAll={handleClear} />

          <SavedSearchesPanel
            currentFilters={filters}
            hasActiveFilters={hasActiveFilters}
            onApply={handleApplySavedSearch}
          />
        </Stack>
      </Form>

      <RecentlyViewedSection />

      {/* Results count step */}
      <SearchResultsHeader resultCount={searchQuery.data?.totalCount} isSearching={searchQuery.isPending} />

      {searchQuery.isPending ? (
        <Stack direction="row" flexWrap="wrap" gap={3} alignItems="stretch">
          {Array.from({ length: SKELETON_COUNT }, (_, index) => (
            <TutorCardSkeleton key={index} />
          ))}
        </Stack>
      ) : null}

      {searchQuery.isError ? (
        <UnavailableState
          headingComponent="h2"
          title="We couldn't load Tutors"
          description="Something went wrong searching for Tutors. You can try again, clear your filters, or head back home."
          actions={[
            { label: "Try again", onClick: () => void searchQuery.refetch() },
            ...(hasActiveFilters
              ? [{ label: "Clear filters", onClick: handleClear }]
              : []),
            { label: "Go Home", to: paths.home, variant: "contained" as const },
          ]}
        />
      ) : null}

      {searchQuery.isSuccess && tutors.length === 0 ? (
        <EmptyState
          title="No Tutors match these filters"
          description={
            hasActiveFilters
              ? "Try broadening or clearing a filter above."
              : "Check back soon — no Tutors are registered yet."
          }
          icon={<SearchRoundedIcon />}
          action={
            hasActiveFilters ? (
              <Button variant="contained" onClick={handleClear}>
                Reset Filters
              </Button>
            ) : undefined
          }
        />
      ) : null}

      {searchQuery.isSuccess && tutors.length > 0 ? (
        <>
          <Stack direction="row" flexWrap="wrap" gap={3} alignItems="stretch">
            {tutors.map((tutor) => (
              <TutorCard
                key={tutor.tutorId}
                tutor={tutor}
                compare={{
                  isSelected: compareIds.includes(tutor.tutorId),
                  onToggle: () => toggleCompare(tutor.tutorId),
                  disabled: compareIds.length >= MAX_COMPARE_COUNT && !compareIds.includes(tutor.tutorId),
                }}
              />
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

      <CompareBar
        selectedTutors={compareTutors}
        onRemove={(tutorId) => setCompareIds((previous) => previous.filter((id) => id !== tutorId))}
        onClear={() => setCompareIds([])}
      />
    </Stack>
  );
}
