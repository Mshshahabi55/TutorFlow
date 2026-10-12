export type LessonStatus = 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled' | 'NoShow';

/** Mirrors `GetMyLessons.Summary` (Lessons module). `tutorUserId` is the only tutor identifier
 * available today — no tutor display name/photo is composed in here, since no cross-module
 * lookup for that exists yet (see MyLessonsView's honest placeholder, not a fabricated name). */
export interface LessonSummary {
  id: string;
  bookingId: string;
  tutorUserId: string;
  status: LessonStatus;
  startUtc: string;
  endUtc: string;
  durationMinutes: number;
}

export interface MyLessonsResponse {
  lessons: LessonSummary[];
}
