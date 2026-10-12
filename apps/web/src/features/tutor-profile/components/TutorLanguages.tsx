import { Skeleton } from '@/components/ui/Skeleton';
import { useLanguages } from '@/features/catalog/useCatalog';
import { SpokenLanguageDto } from '../../search/types';
import { ProfileSection } from './ProfileSection';
import styles from './TutorLanguages.module.css';

export function TutorLanguages({ spokenLanguages }: { spokenLanguages: SpokenLanguageDto[] }) {
  const { data: languages, isLoading } = useLanguages();

  if (spokenLanguages.length === 0) {
    return null;
  }

  if (isLoading) {
    return (
      <ProfileSection title="Languages">
        <Skeleton width="50%" />
      </ProfileSection>
    );
  }

  return (
    <ProfileSection title="Languages">
      <ul className={styles.list}>
        {spokenLanguages.map((language) => (
          <li key={language.languageCode} className={styles.item}>
            <span className={styles.name}>
              {languages?.find((l) => l.code === language.languageCode)?.displayName ?? language.languageCode}
            </span>
            <span className={styles.proficiency}>{language.proficiency}</span>
          </li>
        ))}
      </ul>
    </ProfileSection>
  );
}
