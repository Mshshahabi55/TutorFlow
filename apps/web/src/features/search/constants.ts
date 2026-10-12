/**
 * Subject/specialty/language picker options now come from the real `Subjects` catalog
 * endpoints (`features/catalog/useCatalog.ts`) — ADR-0021 replaced the placeholder static
 * lists that lived here. Country-of-birth has no backend catalog (no product requirement for
 * admin-curated countries), so it stays a small static reference list — a frontend-only UI
 * convenience, not seed/reference data standing in for a missing endpoint.
 */
export const COUNTRY_OPTIONS = [
  { value: 'ES', label: 'Spain' },
  { value: 'FR', label: 'France' },
  { value: 'DE', label: 'Germany' },
  { value: 'GB', label: 'United Kingdom' },
  { value: 'US', label: 'United States' },
  { value: 'PT', label: 'Portugal' },
  { value: 'IT', label: 'Italy' },
  { value: 'MX', label: 'Mexico' },
];

export const RATING_OPTIONS = [
  { value: '4.5', label: '4.5 & up' },
  { value: '4', label: '4.0 & up' },
  { value: '3.5', label: '3.5 & up' },
];
