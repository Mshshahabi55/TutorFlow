import { Chip } from '@/components/ui/Chip';
import { Skeleton } from '@/components/ui/Skeleton';
import { useSpecialties, useSubjects } from '@/features/catalog/useCatalog';
import { ProfileSection } from './ProfileSection';
import styles from './TutorSubjectsAndSpecialties.module.css';

export function TutorSubjectsAndSpecialties({
  subjectIds,
  specialtyIds,
}: {
  subjectIds: string[];
  specialtyIds: string[];
}) {
  const { data: subjects, isLoading: subjectsLoading } = useSubjects();
  const { data: specialties, isLoading: specialtiesLoading } = useSpecialties();

  if (subjectIds.length === 0 && specialtyIds.length === 0) {
    return null;
  }

  if (subjectsLoading || specialtiesLoading) {
    return (
      <ProfileSection title="Subjects">
        <Skeleton width="60%" />
      </ProfileSection>
    );
  }

  const subjectNames = subjectIds
    .map((id) => subjects?.find((s) => s.id === id)?.displayName)
    .filter((name): name is string => Boolean(name));
  const specialtyNames = specialtyIds
    .map((id) => specialties?.find((s) => s.id === id)?.displayName)
    .filter((name): name is string => Boolean(name));

  return (
    <ProfileSection title="Subjects & specialties">
      <div className={styles.chips}>
        {subjectNames.map((name) => (
          <Chip key={name}>{name}</Chip>
        ))}
        {specialtyNames.map((name) => (
          <Chip key={name} variant="neutral">
            {name}
          </Chip>
        ))}
      </div>
    </ProfileSection>
  );
}
