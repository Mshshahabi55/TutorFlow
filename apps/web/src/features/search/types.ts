export type SortOption = 'relevance' | 'ratingDesc' | 'priceAsc' | 'priceDesc' | 'newest';

export const SORT_OPTIONS: { value: SortOption; label: string }[] = [
  { value: 'relevance', label: 'Best match' },
  { value: 'ratingDesc', label: 'Highest rated' },
  { value: 'priceAsc', label: 'Price: low to high' },
  { value: 'priceDesc', label: 'Price: high to low' },
  { value: 'newest', label: 'Newest' },
];

export const MAX_PAGE_SIZE = 50;
export const DEFAULT_PAGE_SIZE = 20;

/**
 * Client-side filter state — mirrors the ID-based Search contract (ADR-0021,
 * docs/design/PREPLY_SEARCH_PARITY_MATRIX.md). Catalog concepts (subject, specialty) are
 * stable IDs resolved against `Subjects`' real catalog endpoints (useCatalog.ts), never
 * display-name strings — the original V1 implementation used names because `Tutors`/`Subjects`
 * had no real catalog yet; that gap is now closed.
 */
export interface SearchFilters {
  q: string;
  subjectId: string;
  specialtyIds: string[];
  languages: string[];
  nativeSpeakerOf: string;
  countryOfBirthCode: string;
  professionalTutor: boolean;
  superTutor: boolean;
  priceMinMinorUnits: number | null;
  priceMaxMinorUnits: number | null;
  minRating: number | null;
  availableSoon: boolean;
  sort: SortOption;
  page: number;
  pageSize: number;
}

export const DEFAULT_FILTERS: SearchFilters = {
  q: '',
  subjectId: '',
  specialtyIds: [],
  languages: [],
  nativeSpeakerOf: '',
  countryOfBirthCode: '',
  professionalTutor: false,
  superTutor: false,
  priceMinMinorUnits: null,
  priceMaxMinorUnits: null,
  minRating: null,
  availableSoon: false,
  sort: 'relevance',
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
};

export interface SpokenLanguageDto {
  languageCode: string;
  proficiency: 'Basic' | 'Conversational' | 'Fluent' | 'Native';
}

export interface TutorSearchResult {
  tutorUserId: string;
  slug: string;
  displayName: string;
  headline: string;
  photoUrl: string | null;
  subjectIds: string[];
  specialtyIds: string[];
  spokenLanguages: SpokenLanguageDto[];
  countryOfBirthCode: string | null;
  isProfessionalTutor: boolean;
  isSuperTutor: boolean;
  hourlyRateMinorUnits: number;
  currency: string;
  averageRating: number;
  reviewCount: number;
  lessonCount: number;
  hasUpcomingAvailability: boolean;
  isAcceptingNewStudents: boolean;
}

export interface SearchTutorsResponse {
  items: TutorSearchResult[];
  page: number;
  pageSize: number;
  totalCount: number;
}
