import { Badge } from '@/components/ui/Badge';

export interface TutorTrustBadgesProps {
  isSuperTutor: boolean;
  isProfessionalTutor: boolean;
}

/**
 * Shared, presentation-only atom — renders bare badges with no wrapping element, so it drops
 * directly into whatever flex/wrap container the caller already has (the search result card's
 * `.tags`, the profile hero's own badge row). Extracted from `TutorResultCard` so both surfaces
 * render the same two platform-determined badges (ADR-0022) from one place, not two copies
 * that can drift. `TutorResultCard` keeps working throughout — this is an additive extraction,
 * not a replace-and-delete.
 */
export function TutorTrustBadges({ isSuperTutor, isProfessionalTutor }: TutorTrustBadgesProps) {
  return (
    <>
      {isSuperTutor && <Badge variant="warning">Super Tutor</Badge>}
      {isProfessionalTutor && <Badge variant="info">Professional Tutor</Badge>}
    </>
  );
}
