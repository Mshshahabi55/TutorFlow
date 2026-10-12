import { DEFAULT_FILTERS, MAX_PAGE_SIZE, SearchFilters, SortOption } from './types';

const SORT_VALUES: SortOption[] = ['relevance', 'ratingDesc', 'priceAsc', 'priceDesc', 'newest'];

/** The UI's camelCase SortOption values (kept for URL aesthetics — `sort=priceAsc` reads better
 * than `sort=PriceAsc`) never matched the backend's PascalCase `SearchTutors.SortOption` enum
 * names, and ASP.NET's `[AsParameters]` query-string enum binding is case-sensitive — every
 * default-sort search request was a 400 before this map existed. Found via live browser
 * verification against the real backend (ADR-0024); no unit test caught it because every
 * existing test either mocks the API or calls it directly with already-correct casing. */
const API_SORT_VALUES: Record<SortOption, string> = {
  relevance: 'Relevance',
  ratingDesc: 'RatingDesc',
  priceAsc: 'PriceAsc',
  priceDesc: 'PriceDesc',
  newest: 'Newest',
};

function parseIntOrNull(value: string | null): number | null {
  if (!value) return null;
  const n = Number.parseInt(value, 10);
  return Number.isFinite(n) ? n : null;
}

/** Negative values are malformed/meaningless for either field, not a deliberate business-rule
 * violation (unlike priceMax < priceMin, which the backend should reject with a visible 400) —
 * clamped to null here for the same reason page/pageSize already are. */
function parseNonNegativeIntOrNull(value: string | null): number | null {
  const n = parseIntOrNull(value);
  return n !== null && n >= 0 ? n : null;
}

function parseFloatOrNull(value: string | null): number | null {
  if (!value) return null;
  const n = Number.parseFloat(value);
  return Number.isFinite(n) ? n : null;
}

/** Out-of-range (outside [0,5]) is treated the same as unparseable — malformed, not a
 * deliberate business-rule violation worth surfacing as a server error. */
function parseRatingOrNull(value: string | null): number | null {
  const n = parseFloatOrNull(value);
  return n !== null && n >= 0 && n <= 5 ? n : null;
}

function parseList(value: string | null): string[] {
  if (!value) return [];
  return value
    .split(',')
    .map((v) => v.trim())
    .filter(Boolean);
}

/**
 * Pure URL -> filters parser. Lenient by design: a user can hand-edit the URL, so malformed
 * values are dropped/clamped silently here rather than thrown — the backend's own validation
 * (SEARCH_SPEC.md §7) is what returns a real 400 for a request that reaches the API with bad
 * values; this function's job is only to never crash the page on a weird URL.
 */
export function parseFiltersFromSearchParams(searchParams: URLSearchParams): SearchFilters {
  const page = parseIntOrNull(searchParams.get('page')) ?? DEFAULT_FILTERS.page;
  const pageSize = parseIntOrNull(searchParams.get('pageSize')) ?? DEFAULT_FILTERS.pageSize;
  const sortParam = searchParams.get('sort');
  const sort = SORT_VALUES.includes(sortParam as SortOption) ? (sortParam as SortOption) : DEFAULT_FILTERS.sort;

  return {
    q: searchParams.get('q') ?? DEFAULT_FILTERS.q,
    subjectId: searchParams.get('subjectId') ?? DEFAULT_FILTERS.subjectId,
    specialtyIds: parseList(searchParams.get('specialtyIds')),
    languages: parseList(searchParams.get('languages')),
    nativeSpeakerOf: searchParams.get('nativeSpeakerOf') ?? DEFAULT_FILTERS.nativeSpeakerOf,
    countryOfBirthCode: searchParams.get('countryOfBirthCode') ?? DEFAULT_FILTERS.countryOfBirthCode,
    professionalTutor: searchParams.get('professionalTutor') === 'true',
    superTutor: searchParams.get('superTutor') === 'true',
    priceMinMinorUnits: parseNonNegativeIntOrNull(searchParams.get('priceMin')),
    priceMaxMinorUnits: parseNonNegativeIntOrNull(searchParams.get('priceMax')),
    minRating: parseRatingOrNull(searchParams.get('minRating')),
    availableSoon: searchParams.get('availableSoon') === 'true',
    sort,
    page: page >= 1 ? page : DEFAULT_FILTERS.page,
    pageSize: pageSize >= 1 && pageSize <= MAX_PAGE_SIZE ? pageSize : DEFAULT_FILTERS.pageSize,
  };
}

/** Filters -> URL. Omits any field at its default so the URL stays as short as possible and
 * a default-state search is just `/find-tutors` with no query string at all. */
export function filtersToSearchParams(filters: SearchFilters): URLSearchParams {
  const params = new URLSearchParams();

  if (filters.q) params.set('q', filters.q);
  if (filters.subjectId) params.set('subjectId', filters.subjectId);
  if (filters.specialtyIds.length > 0) params.set('specialtyIds', filters.specialtyIds.join(','));
  if (filters.languages.length > 0) params.set('languages', filters.languages.join(','));
  if (filters.nativeSpeakerOf) params.set('nativeSpeakerOf', filters.nativeSpeakerOf);
  if (filters.countryOfBirthCode) params.set('countryOfBirthCode', filters.countryOfBirthCode);
  if (filters.professionalTutor) params.set('professionalTutor', 'true');
  if (filters.superTutor) params.set('superTutor', 'true');
  if (filters.priceMinMinorUnits !== null) params.set('priceMin', String(filters.priceMinMinorUnits));
  if (filters.priceMaxMinorUnits !== null) params.set('priceMax', String(filters.priceMaxMinorUnits));
  if (filters.minRating !== null) params.set('minRating', String(filters.minRating));
  if (filters.availableSoon) params.set('availableSoon', 'true');
  if (filters.sort !== DEFAULT_FILTERS.sort) params.set('sort', filters.sort);
  if (filters.page !== DEFAULT_FILTERS.page) params.set('page', String(filters.page));
  if (filters.pageSize !== DEFAULT_FILTERS.pageSize) params.set('pageSize', String(filters.pageSize));

  return params;
}

/** The same filters, shaped as the backend query-string contract (SearchTutors.Query) for the
 * API client — kept separate from filtersToSearchParams because the two have diverged once
 * already (server always wants `page`/`pageSize` even at defaults; the URL omits them). */
export function filtersToApiParams(filters: SearchFilters): Record<string, string | undefined> {
  return {
    q: filters.q || undefined,
    subjectId: filters.subjectId || undefined,
    specialtyIds: filters.specialtyIds.length > 0 ? filters.specialtyIds.join(',') : undefined,
    spokenLanguageCodes: filters.languages.length > 0 ? filters.languages.join(',') : undefined,
    nativeSpeakerOf: filters.nativeSpeakerOf || undefined,
    countryOfBirthCode: filters.countryOfBirthCode || undefined,
    professionalTutor: filters.professionalTutor ? 'true' : undefined,
    superTutor: filters.superTutor ? 'true' : undefined,
    priceMinMinorUnits: filters.priceMinMinorUnits !== null ? String(filters.priceMinMinorUnits) : undefined,
    priceMaxMinorUnits: filters.priceMaxMinorUnits !== null ? String(filters.priceMaxMinorUnits) : undefined,
    minRating: filters.minRating !== null ? String(filters.minRating) : undefined,
    availableSoon: filters.availableSoon ? 'true' : undefined,
    sort: API_SORT_VALUES[filters.sort],
    page: String(filters.page),
    pageSize: String(filters.pageSize),
  };
}

export function countActiveFilters(filters: SearchFilters): number {
  let count = 0;
  if (filters.q) count++;
  if (filters.subjectId) count++;
  if (filters.specialtyIds.length > 0) count++;
  if (filters.languages.length > 0) count++;
  if (filters.nativeSpeakerOf) count++;
  if (filters.countryOfBirthCode) count++;
  if (filters.professionalTutor) count++;
  if (filters.superTutor) count++;
  if (filters.priceMinMinorUnits !== null || filters.priceMaxMinorUnits !== null) count++;
  if (filters.minRating !== null) count++;
  if (filters.availableSoon) count++;
  return count;
}
