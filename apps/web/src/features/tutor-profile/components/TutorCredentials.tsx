import { CertificateEntryDto, EducationEntryDto } from '../types';
import { ProfileSection } from './ProfileSection';
import styles from './TutorCredentials.module.css';

export interface TutorCredentialsProps {
  yearsOfExperience: number | null;
  experienceSummary: string | null;
  educationEntries: EducationEntryDto[];
  certificateEntries: CertificateEntryDto[];
}

/**
 * Combines experience + education + certificates into one "resume" block — Preply shows these
 * as one combined section too (PREPLY_TUTOR_PROFILE_PARITY_SPEC.md §7); a presentation
 * simplification, not a data loss (every field below is still shown).
 */
export function TutorCredentials({
  yearsOfExperience,
  experienceSummary,
  educationEntries,
  certificateEntries,
}: TutorCredentialsProps) {
  const hasContent =
    yearsOfExperience !== null ||
    Boolean(experienceSummary) ||
    educationEntries.length > 0 ||
    certificateEntries.length > 0;

  if (!hasContent) {
    return null;
  }

  return (
    <ProfileSection title="Experience & credentials">
      {(yearsOfExperience !== null || experienceSummary) && (
        <p className={styles.summary}>
          {yearsOfExperience !== null && (
            <>
              {yearsOfExperience} year{yearsOfExperience === 1 ? '' : 's'} of teaching experience.{' '}
            </>
          )}
          {experienceSummary}
        </p>
      )}

      {educationEntries.length > 0 && (
        <>
          <h3 className={styles.subheading}>Education</h3>
          <ul className={styles.entryList}>
            {educationEntries.map((entry, index) => (
              <li key={index} className={styles.entry}>
                <div className={styles.entryPrimary}>
                  {entry.degree}
                  {entry.fieldOfStudy ? `, ${entry.fieldOfStudy}` : ''}
                </div>
                <div className={styles.entrySecondary}>
                  {entry.institution}
                  {entry.startYear ? ` · ${entry.startYear}–${entry.endYear ?? 'present'}` : ''}
                </div>
              </li>
            ))}
          </ul>
        </>
      )}

      {certificateEntries.length > 0 && (
        <>
          <h3 className={styles.subheading}>Certificates</h3>
          <ul className={styles.entryList}>
            {certificateEntries.map((entry, index) => (
              <li key={index} className={styles.entry}>
                <div className={styles.entryPrimary}>{entry.name}</div>
                <div className={styles.entrySecondary}>
                  {entry.issuingOrganization}
                  {entry.issueYear ? ` · ${entry.issueYear}` : ''}
                </div>
              </li>
            ))}
          </ul>
        </>
      )}
    </ProfileSection>
  );
}
