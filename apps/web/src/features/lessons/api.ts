import { apiGet } from '@/lib/api/client';
import { MyLessonsResponse } from './types';

export function getMyLessons(signal?: AbortSignal): Promise<MyLessonsResponse> {
  return apiGet<MyLessonsResponse>('/api/lessons/me', undefined, { signal });
}
