import { apiGet } from '@/lib/api/client';
import { LanguageDto, SpecialtyDto, SubjectDto } from './types';

export function getSubjects(options?: { signal?: AbortSignal }): Promise<SubjectDto[]> {
  return apiGet<SubjectDto[]>('/api/subjects', undefined, options);
}

export function getSpecialties(
  subjectId?: string,
  options?: { signal?: AbortSignal },
): Promise<SpecialtyDto[]> {
  return apiGet<SpecialtyDto[]>('/api/specialties', { subjectId }, options);
}

export function getLanguages(options?: { signal?: AbortSignal }): Promise<LanguageDto[]> {
  return apiGet<LanguageDto[]>('/api/languages', undefined, options);
}
