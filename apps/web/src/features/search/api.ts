import { apiGet } from '@/lib/api/client';
import { filtersToApiParams } from './filterSerialization';
import { SearchFilters, SearchTutorsResponse } from './types';

export function searchTutors(
  filters: SearchFilters,
  options?: { signal?: AbortSignal },
): Promise<SearchTutorsResponse> {
  return apiGet<SearchTutorsResponse>('/api/search/tutors', filtersToApiParams(filters), options);
}
