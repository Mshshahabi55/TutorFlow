import { describe, expect, it } from 'vitest';
import {
  countActiveFilters,
  filtersToApiParams,
  filtersToSearchParams,
  parseFiltersFromSearchParams,
} from './filterSerialization';
import { DEFAULT_FILTERS, SearchFilters } from './types';

describe('parseFiltersFromSearchParams', () => {
  it('returns all defaults for an empty query string', () => {
    const filters = parseFiltersFromSearchParams(new URLSearchParams(''));
    expect(filters).toEqual(DEFAULT_FILTERS);
  });

  it('parses every supported field from the URL', () => {
    const params = new URLSearchParams(
      'q=maria&subjectId=11111111-1111-1111-1111-111111111111' +
        '&specialtyIds=22222222-2222-2222-2222-222222222222,33333333-3333-3333-3333-333333333333' +
        '&languages=es,en&nativeSpeakerOf=es&countryOfBirthCode=ES&professionalTutor=true&superTutor=true' +
        '&priceMin=1000&priceMax=5000&minRating=4.5&availableSoon=true&sort=ratingDesc&page=2&pageSize=10',
    );

    const filters = parseFiltersFromSearchParams(params);

    expect(filters).toEqual({
      q: 'maria',
      subjectId: '11111111-1111-1111-1111-111111111111',
      specialtyIds: ['22222222-2222-2222-2222-222222222222', '33333333-3333-3333-3333-333333333333'],
      languages: ['es', 'en'],
      nativeSpeakerOf: 'es',
      countryOfBirthCode: 'ES',
      professionalTutor: true,
      superTutor: true,
      priceMinMinorUnits: 1000,
      priceMaxMinorUnits: 5000,
      minRating: 4.5,
      availableSoon: true,
      sort: 'ratingDesc',
      page: 2,
      pageSize: 10,
    } satisfies SearchFilters);
  });

  it('is lenient about malformed values instead of throwing', () => {
    const params = new URLSearchParams('page=not-a-number&pageSize=9999&sort=not-a-real-sort&minRating=oops');

    const filters = parseFiltersFromSearchParams(params);

    expect(filters.page).toBe(DEFAULT_FILTERS.page);
    expect(filters.pageSize).toBe(DEFAULT_FILTERS.pageSize);
    expect(filters.sort).toBe(DEFAULT_FILTERS.sort);
    expect(filters.minRating).toBeNull();
  });

  it('clamps an out-of-range pageSize back to the default rather than accepting it', () => {
    const filters = parseFiltersFromSearchParams(new URLSearchParams('pageSize=51'));
    expect(filters.pageSize).toBe(DEFAULT_FILTERS.pageSize);
  });

  it('treats a negative page as invalid and falls back to the default', () => {
    const filters = parseFiltersFromSearchParams(new URLSearchParams('page=-3'));
    expect(filters.page).toBe(DEFAULT_FILTERS.page);
  });

  it('treats a negative price as malformed and drops it to null, not a literal negative filter', () => {
    const filters = parseFiltersFromSearchParams(new URLSearchParams('priceMin=-500'));
    expect(filters.priceMinMinorUnits).toBeNull();
  });

  it('treats an out-of-[0,5]-range rating as malformed and drops it to null', () => {
    const filters = parseFiltersFromSearchParams(new URLSearchParams('minRating=99'));
    expect(filters.minRating).toBeNull();
  });
});

describe('filtersToSearchParams', () => {
  it('produces an empty query string for all-default filters', () => {
    const params = filtersToSearchParams(DEFAULT_FILTERS);
    expect(params.toString()).toBe('');
  });

  it('round-trips through parse -> serialize -> parse', () => {
    const original: SearchFilters = {
      q: 'english',
      subjectId: '11111111-1111-1111-1111-111111111111',
      specialtyIds: ['22222222-2222-2222-2222-222222222222'],
      languages: ['en', 'fr'],
      nativeSpeakerOf: 'en',
      countryOfBirthCode: 'FR',
      professionalTutor: true,
      superTutor: false,
      priceMinMinorUnits: 1500,
      priceMaxMinorUnits: 4000,
      minRating: 4,
      availableSoon: true,
      sort: 'priceAsc',
      page: 3,
      pageSize: 30,
    };

    const roundTripped = parseFiltersFromSearchParams(filtersToSearchParams(original));

    expect(roundTripped).toEqual(original);
  });

  it('omits page and pageSize from the URL when they are at their defaults', () => {
    const params = filtersToSearchParams({ ...DEFAULT_FILTERS, q: 'maria' });
    expect(params.has('page')).toBe(false);
    expect(params.has('pageSize')).toBe(false);
    expect(params.get('q')).toBe('maria');
  });
});

describe('filtersToApiParams', () => {
  it('always includes page and pageSize for the backend, unlike the URL serializer', () => {
    const params = filtersToApiParams(DEFAULT_FILTERS);
    expect(params.page).toBe('1');
    expect(params.pageSize).toBe(String(DEFAULT_FILTERS.pageSize));
  });

  it('joins array filters with commas matching the backend query contract', () => {
    const params = filtersToApiParams({
      ...DEFAULT_FILTERS,
      specialtyIds: ['A', 'B'],
      languages: ['en'],
    });
    expect(params.specialtyIds).toBe('A,B');
    expect(params.spokenLanguageCodes).toBe('en');
  });

  it('maps badge filters to the backend professionalTutor/superTutor param names', () => {
    const params = filtersToApiParams({ ...DEFAULT_FILTERS, professionalTutor: true, superTutor: true });
    expect(params.professionalTutor).toBe('true');
    expect(params.superTutor).toBe('true');
  });

  it('omits empty/null fields entirely rather than sending empty-string params', () => {
    const params = filtersToApiParams(DEFAULT_FILTERS);
    expect(params.q).toBeUndefined();
    expect(params.priceMinMinorUnits).toBeUndefined();
    expect(params.professionalTutor).toBeUndefined();
  });

  it('maps every UI sort value to the backend\'s PascalCase SortOption enum name — regression for a real 400 found via live browser verification', () => {
    const cases: [SearchFilters['sort'], string][] = [
      ['relevance', 'Relevance'],
      ['ratingDesc', 'RatingDesc'],
      ['priceAsc', 'PriceAsc'],
      ['priceDesc', 'PriceDesc'],
      ['newest', 'Newest'],
    ];
    for (const [uiValue, backendValue] of cases) {
      expect(filtersToApiParams({ ...DEFAULT_FILTERS, sort: uiValue }).sort).toBe(backendValue);
    }
  });
});

describe('countActiveFilters', () => {
  it('is zero for default filters', () => {
    expect(countActiveFilters(DEFAULT_FILTERS)).toBe(0);
  });

  it('counts a price range (min and/or max) as a single active filter', () => {
    expect(countActiveFilters({ ...DEFAULT_FILTERS, priceMinMinorUnits: 1000 })).toBe(1);
    expect(countActiveFilters({ ...DEFAULT_FILTERS, priceMinMinorUnits: 1000, priceMaxMinorUnits: 2000 })).toBe(1);
  });

  it('counts each distinct dimension independently, including the new badge filters', () => {
    const count = countActiveFilters({
      ...DEFAULT_FILTERS,
      subjectId: '11111111-1111-1111-1111-111111111111',
      languages: ['en'],
      availableSoon: true,
      professionalTutor: true,
      superTutor: true,
    });
    expect(count).toBe(5);
  });
});
