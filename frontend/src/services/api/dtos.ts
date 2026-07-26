// Mirrors the backend's Application-layer DTOs exactly (src/Application/**/DTOs).
// No field is added, renamed, or reshaped — this is a direct TypeScript
// projection of the already-approved, already-implemented API contract.
// Feature modules import from here rather than redefining these shapes.

/** Mirrors TutorFlow.Domain.Scheduling.ValueObjects.DeliveryMode. */
export enum DeliveryMode {
  Online = 0,
  InPerson = 1,
}

/** Mirrors TutorFlow.Domain.Scheduling.ValueObjects.SessionStatus. */
export enum SessionStatus {
  Scheduled = 0,
  Completed = 1,
  Cancelled = 2,
  NoShow = 3,
}

/** Mirrors TutorFlow.Domain.Identity.ValueObjects.RelationshipStatus. */
export enum RelationshipStatus {
  Invited = 0,
  Confirmed = 1,
}

/**
 * A .NET TimeSpan, serialized in "c" format (e.g. "01:00:00"). No parsing
 * utility is provided here — none is needed until a feature that displays
 * or edits a duration is implemented.
 */
export type TimeSpanString = string;

/** An ISO 8601 UTC timestamp string, as System.Text.Json serializes DateTime. */
export type IsoDateTimeString = string;

export interface TutorDto {
  tutorId: string;
  isApproved: boolean;
  isSuspended: boolean;
  isDiscoverable: boolean;
  hourlyRate: number | null;
  subject: string | null;
  language: string | null;
  location: string | null;
  offeredDurations: TimeSpanString[];
}

export interface StudentDto {
  studentId: string;
  isMinor: boolean;
}

export interface ParentGuardianDto {
  parentGuardianId: string;
}

export interface LoginResultDto {
  token: string;
  accountId: string;
  role: string;
  expiresAtUtc: IsoDateTimeString;
}

export interface RelationshipDto {
  relationshipId: string;
  parentGuardianId: string;
  studentId: string;
  status: RelationshipStatus;
}

export interface AvailabilitySlotDto {
  availabilitySlotId: string;
  tutorId: string;
  startTimeUtc: IsoDateTimeString;
  endTimeUtc: IsoDateTimeString;
  duration: TimeSpanString;
  deliveryMode: DeliveryMode;
  isConsumed: boolean;
}

export interface SessionDto {
  sessionId: string;
  tutorId: string;
  studentId: string;
  parentGuardianId: string | null;
  availabilitySlotId: string;
  scheduledTimeUtc: IsoDateTimeString;
  endTimeUtc: IsoDateTimeString;
  duration: TimeSpanString;
  deliveryMode: DeliveryMode;
  status: SessionStatus;
}
