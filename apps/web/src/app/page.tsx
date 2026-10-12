import styles from './page.module.css';
import { PageContainer } from '@/components/layout/PageContainer';
import { ButtonLink } from '@/components/ui/ButtonLink';

export default function HomePage() {
  return (
    <PageContainer>
      <section className={styles.hero}>
        <h1 className={styles.title}>Find a tutor you&rsquo;ll actually enjoy learning from</h1>
        <p className={styles.subtitle}>
          TutorFlow connects students with vetted tutors for live, 1:1 online lessons — browse
          subjects, book a trial lesson, and start learning today.
        </p>
        <div className={styles.actions}>
          <ButtonLink href="/find-tutors" size="lg">
            Find a tutor
          </ButtonLink>
          <ButtonLink href="/become-a-tutor" variant="secondary" size="lg">
            Become a tutor
          </ButtonLink>
        </div>
      </section>
    </PageContainer>
  );
}
