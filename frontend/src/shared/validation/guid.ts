import { z } from "zod";

// No lookup-by-name capability exists at the API boundary for any entity in
// any feature module (only lookup-by-id) — every id field across the app is
// a raw GUID the user supplies directly, mirroring the backend's own
// Guid.Empty structural check (e.g. GetTutorByIdQueryValidator). Promoted
// here from features/identity once features/scheduling needed the same
// pattern (Sprint 7) — a feature-to-feature import would have violated
// feature-first boundaries.
const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export const guidSchema = z
  .string()
  .trim()
  .min(1, "An id is required.")
  .regex(GUID_PATTERN, "Enter a valid id (a GUID, e.g. 00000000-0000-0000-0000-000000000000).");

export const idLookupSchema = z.object({ id: guidSchema });

export type IdLookupFormValues = z.infer<typeof idLookupSchema>;
