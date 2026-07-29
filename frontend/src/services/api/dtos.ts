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

/** ADR-024 (Accepted, 2026-07-28): a Subject/Level pair, additive alongside `TutorDto.subject`. */
export interface TutorSubjectDto {
  subject: string;
  level: string | null;
}

/** ADR-024 (Accepted, 2026-07-28): governs only whether a Tutor has entered the existing Admin approval queue — never a replacement for isApproved/isSuspended. */
export type TutorProfileStatus = "Draft" | "Submitted";

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
  // ADR-024 (Accepted, 2026-07-28) — Tutor Profile Enrichment. Every field
  // below is self-declared and unverified (see the ADR's own Verification
  // section) — never fabricated by the frontend when absent. Optional here
  // (the real API always sends them) specifically so the dozens of
  // pre-existing test fixtures built before this ADR don't all need a
  // mechanical update just to satisfy the type — every real caller of this
  // DTO already gets them; only object literals that predate this change
  // are missing them.
  profileStatus?: TutorProfileStatus;
  displayName?: string | null;
  headline?: string | null;
  biography?: string | null;
  country?: string | null;
  city?: string | null;
  otherLanguages?: string[];
  tutorSubjects?: TutorSubjectDto[];
  yearsOfExperience?: number | null;
  education?: string | null;
  certifications?: string | null;
  teachingMethodology?: string | null;
  lessonSpecialties?: string[];
  photoUrl?: string | null;
  introVideoUrl?: string | null;
  galleryImageUrls?: string[];
  trialLessonAvailable?: boolean;
  trialLessonPrice?: number | null;
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

/** GET /sessions/status-counts (Marketplace Oversight, Admin dashboard KPI) — platform-wide Session counts grouped by status. */
export interface SessionStatusCountsDto {
  scheduled: number;
  completed: number;
  cancelled: number;
  noShow: number;
}

/** Mirrors TutorFlow.Domain.Communication.ValueObjects.NotificationType. No JsonStringEnumConverter is registered, so this serializes as its underlying int, same as DeliveryMode/SessionStatus/RelationshipStatus above. */
export enum NotificationType {
  BookingConfirmed = 0,
  LessonCancelled = 1,
  TutorReplied = 2,
  NewMessage = 3,
  ParentConfirmed = 4,
  AvailabilityChanged = 5,
}

export interface ConversationDto {
  conversationId: string;
  otherParticipantId: string;
  createdAtUtc: IsoDateTimeString;
  lastMessageAtUtc: IsoDateTimeString | null;
  lastMessagePreview: string | null;
  unreadCount: number;
}

export interface MessageDto {
  messageId: string;
  conversationId: string;
  senderId: string;
  recipientId: string;
  body: string;
  sentAtUtc: IsoDateTimeString;
  readAtUtc: IsoDateTimeString | null;
}

export interface NotificationDto {
  notificationId: string;
  type: NotificationType;
  summary: string;
  relatedEntityId: string | null;
  createdAtUtc: IsoDateTimeString;
  readAtUtc: IsoDateTimeString | null;
}

/** Mirrors TutorFlow.Domain.Meetings.ValueObjects.MeetingProviderOption. Mock is Development/Testing only — never a legitimate value in production (docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md). */
export enum MeetingProviderOption {
  GoogleMeet = 0,
  MicrosoftTeams = 1,
  Zoom = 2,
  Mock = 3,
}

/** Mirrors TutorFlow.Domain.Meetings.ValueObjects.MeetingStatus. */
export enum MeetingStatus {
  Scheduled = 0,
  Cancelled = 1,
}

export interface MeetingDto {
  meetingId: string;
  sessionId: string;
  provider: MeetingProviderOption;
  joinUrl: string;
  hostUrl: string | null;
  startsAtUtc: IsoDateTimeString;
  endsAtUtc: IsoDateTimeString;
  status: MeetingStatus;
  createdAtUtc: IsoDateTimeString;
  updatedAtUtc: IsoDateTimeString;
}
