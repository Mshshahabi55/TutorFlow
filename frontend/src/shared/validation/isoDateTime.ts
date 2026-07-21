import { z } from "zod";

/**
 * Every Scheduling & Booking timestamp (DeclareAvailability's StartTimeUtc,
 * RescheduleSession's NewScheduledTimeUtc) is a .NET DateTime the backend
 * stores and returns in UTC. Per this sprint's instruction not to invent
 * timezone-conversion logic, the UI takes and displays this value as a
 * literal UTC ISO 8601 string — no local-timezone picker, no client-side
 * offset math. What the user types is exactly what the backend receives.
 */
const ISO_UTC_PATTERN = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z$/;

export const isoDateTimeUtcSchema = z
  .string()
  .trim()
  .min(1, "A date/time is required.")
  .regex(
    ISO_UTC_PATTERN,
    "Enter a UTC date/time in ISO 8601 format, e.g. 2026-08-01T14:00:00Z.",
  )
  .refine((value) => !Number.isNaN(Date.parse(value)), "Enter a valid date/time.");
