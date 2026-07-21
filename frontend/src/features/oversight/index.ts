// Marketplace Oversight feature module. Owns exactly one capability of its
// own — GET /sessions, view all schedules (ADM-3). Tutor approval/suspension
// (ADM-1, ADM-2) are Identity & Relationship's own capabilities (Sprint 6);
// this module's Admin dashboard composes their existing hooks rather than
// re-implementing them. Implemented in Sprint 8. Pages are imported directly
// by src/routes/router.tsx (each behind its own React.lazy()) — see
// src/features/identity/index.ts for the same convention.
export {};
