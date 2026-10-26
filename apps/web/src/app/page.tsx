'use client';

import Link from 'next/link';
import { useSetHeaderVariant } from '@/components/layout/HeaderVariantContext';
import { PageContainer } from '@/components/layout/PageContainer';
import { ButtonLink } from '@/components/ui/ButtonLink';
import { useSubjects } from '@/features/catalog/useCatalog';
import styles from './page.module.css';

/** Qualitative, non-numeric trust statements — TutorFlow has no verified usage metrics to quote
 * (unlike the real Preply homepage's "100,000+ tutors" strip), so these describe real, already-
 * built platform capabilities instead of inventing numbers. */
const TRUST_ITEMS = [
  { title: 'Vetted tutors', label: 'Reviewed before they teach' },
  { title: 'Live 1:1 lessons', label: 'Real-time video, not pre-recorded' },
  { title: 'Any subject', label: 'From languages to test prep' },
  { title: 'Flexible scheduling', label: 'Book around your life' },
  { title: 'Trial lessons', label: 'Try before you commit' },
];

/** Original copy describing TutorFlow's own real booking flow (not Preply's wording) — matches
 * the real Preply homepage's numbered "How it works" structure. */
const HOW_IT_WORKS_STEPS = [
  {
    title: 'Find your tutor',
    body: 'Browse real tutor profiles — filter by subject, language, price, and availability until one feels right.',
  },
  {
    title: 'Book a trial lesson',
    body: 'Pick a time that works for you and meet live over video before committing to regular lessons.',
  },
  {
    title: 'Keep learning',
    body: 'Liked the fit? Book regular lessons on a schedule that works around your life.',
  },
];

function MathIcon() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
      <path d="M5 4h14L12 12l7 8H5" />
    </svg>
  );
}

function LanguageIcon() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
      <path d="M4 19V7a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H9l-5 4Z" />
    </svg>
  );
}

export default function HomePage() {
  // Pink nav + dark announcement strip, matching the real Preply homepage's header/hero band —
  // homepage-only (SiteHeader stays its normal white self on every other route).
  useSetHeaderVariant('home');

  // Real catalog data — the actual subjects TutorFlow currently offers, not a fabricated list.
  // No per-subject tutor counts: there's no cheap aggregate endpoint for that today, and
  // guessing a number would be exactly the kind of fabricated metric this page avoids elsewhere.
  const { data: subjects, isLoading: subjectsLoading } = useSubjects();

  return (
    <>
      <section className={styles.hero}>
        <PageContainer className={styles.heroInner}>
          <div className={styles.heroText}>
            <h1 className={styles.title}>
              Learn faster
              <br />
              with a tutor
              <br />
              you&rsquo;ll love.
            </h1>
            <ButtonLink href="/find-tutors" className={styles.heroCta}>
              Find a tutor
              <span aria-hidden="true">→</span>
            </ButtonLink>
          </div>
          <div className={styles.heroImage}>
            <img src="/images/home/hero-layer.jpg" alt="" aria-hidden="true" className={styles.heroImageLayer} />
            <img
              src="/images/home/hero-main.jpg"
              alt="A tutor smiling and waving hello during a live video lesson"
              className={styles.heroImageMain}
            />
            <img src="/images/home/hero-inset.jpg" alt="" aria-hidden="true" className={styles.heroImageInset} />
          </div>
        </PageContainer>
      </section>

      <section className={styles.trust}>
        <PageContainer className={styles.trustInner}>
          {TRUST_ITEMS.map((item) => (
            <div key={item.title} className={styles.trustItem}>
              <p className={styles.trustTitle}>{item.title}</p>
              <p className={styles.trustLabel}>{item.label}</p>
            </div>
          ))}
        </PageContainer>
      </section>

      {/* Real subjects from the catalog API, not a fabricated list — matches the real Preply
          homepage's language/subject grid structurally (icon, name, chevron), without the
          per-card teacher counts (no cheap real aggregate exists for that yet). */}
      {!subjectsLoading && subjects && subjects.length > 0 && (
        <section className={styles.subjects}>
          <PageContainer>
            <h2 className={styles.sectionTitle}>Popular subjects</h2>
            <ul className={styles.subjectGrid}>
              {subjects.map((subject) => (
                <li key={subject.id}>
                  <Link href={`/find-tutors?subjectId=${subject.id}`} className={styles.subjectCard}>
                    <span className={styles.subjectIcon} aria-hidden="true">
                      {subject.code === 'mathematics' ? <MathIcon /> : <LanguageIcon />}
                    </span>
                    <span className={styles.subjectName}>{subject.displayName} tutors</span>
                    <span className={styles.subjectChevron} aria-hidden="true">
                      ›
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          </PageContainer>
        </section>
      )}

      <section className={styles.howItWorks}>
        <PageContainer>
          <h2 className={styles.sectionTitle}>How TutorFlow works</h2>
          <ol className={styles.stepsGrid}>
            {HOW_IT_WORKS_STEPS.map((step, index) => (
              <li key={step.title} className={styles.stepCard}>
                <span className={styles.stepNumber} aria-hidden="true">
                  {index + 1}
                </span>
                <h3 className={styles.stepTitle}>{step.title}</h3>
                <p className={styles.stepBody}>{step.body}</p>
              </li>
            ))}
          </ol>
        </PageContainer>
      </section>

      {/* Truthful description of the real trial-lesson + cancellation policy (ADR-0027,
          TRIAL_BOOKING_WORKFLOW.md: paid trial, full refund 24h+ before the lesson) — not
          Preply's "try another tutor for free" guarantee, which TutorFlow doesn't offer. */}
      <section className={styles.trial}>
        <PageContainer className={styles.trialInner}>
          <h2 className={styles.sectionTitle}>Try before you commit</h2>
          <p className={styles.trialBody}>
            Book a short trial lesson with any tutor to see if they&rsquo;re the right fit. Cancel 24 hours or
            more before your lesson starts for a full refund.
          </p>
          <ButtonLink href="/find-tutors" variant="secondary">
            Browse tutors
          </ButtonLink>
        </PageContainer>
      </section>

      <section className={styles.becomeTutor}>
        <PageContainer className={styles.becomeTutorInner}>
          <div>
            <h2 className={styles.sectionTitle}>Teach what you know</h2>
            <p className={styles.trialBody}>
              Set your own rate, choose your own schedule, and get paid securely for the lessons you teach.
            </p>
          </div>
          <ButtonLink href="/become-a-tutor">
            Become a tutor
            <span aria-hidden="true">→</span>
          </ButtonLink>
        </PageContainer>
      </section>
    </>
  );
}
