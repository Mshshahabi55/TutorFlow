import { z } from "zod";

// Shared by every registration form (Tutor/Student/Parent-Guardian) and the
// Login form — promoted here once needed by both features/identity and
// features/auth, avoiding a feature-to-feature import (mirrors guidSchema's
// own promotion history). Mirrors the backend's own structural checks
// exactly (backend/src/Application/Identity/Validators/CredentialValidation.cs):
// non-empty email, and a password of at least 8 characters — no forced
// complexity/rotation rules on either side.
export const emailSchema = z
  .string()
  .trim()
  .min(1, "Email is required.")
  .email("Enter a valid email address.");

export const passwordSchema = z.string().min(8, "Password must be at least 8 characters.");
