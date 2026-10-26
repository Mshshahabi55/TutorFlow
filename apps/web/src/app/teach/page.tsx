import { PageContainer } from '@/components/layout/PageContainer';
import { ButtonLink } from '@/components/ui/ButtonLink';
import styles from './page.module.css';

export const metadata = { title: 'Teach on TutorFlow | TutorFlow' };

/** Layout/visual reference: the real `preply.com/en/teach` recruitment landing page
 * (docs/product/preply-reverse-engineering/evidence/preply-wizard-evidence-2026-10-18/SS53_teach_landing_desktop.png,
 * a fresh, directly-observed screenshot of the live site). Copy is TutorFlow's own — not Preply's
 * wording verbatim — and omits any approval-time claim, since no such SLA is an established
 * TutorFlow business rule (unlike Preply's own evidence, which isn't even internally consistent
 * about it — see docs/product/preply-reverse-engineering/evidence/playwright-tutor-onboarding-2026-10-18/comparison-with-existing-evidence.md). */
const STEPS = [
  {
    title: 'Sign up',
    body: 'Create your tutor profile — it only takes a few minutes to get started.',
  },
  {
    title: 'Get approved',
    body: 'Our team reviews every application before it goes live.',
  },
  {
    title: 'Start earning',
    body: 'Set your own rate and teach students on a schedule that works for you.',
  },
] as const;

export default function TeachPage() {
  return (
    <section className={styles.hero}>
      <PageContainer className={styles.heroInner}>
        <div className={styles.heroText}>
          <h1 className={styles.title}>Make a living teaching learners around the world.</h1>

          <ol className={styles.stepsList}>
            {STEPS.map((step, index) => (
              <li key={step.title} className={styles.stepItem}>
                <span className={`${styles.stepNumber} ${index === 0 ? styles.stepNumberActive : ''}`} aria-hidden="true">
                  {index + 1}
                </span>
                <h2 className={styles.stepTitle}>{step.title}</h2>
                <p className={styles.stepBody}>{step.body}</p>
              </li>
            ))}
          </ol>

          <ButtonLink href="/become-a-tutor" size="lg" className={styles.cta}>
            Create a tutor profile now
          </ButtonLink>
        </div>

        <div className={styles.heroImage}>
          <img src="/images/home/hero-inset.jpg" alt="" aria-hidden="true" className={styles.heroImageBack2} />
          <img src="/images/home/hero-layer.jpg" alt="" aria-hidden="true" className={styles.heroImageBack1} />
          <img
            src="/images/home/hero-main.jpg"
            alt="A tutor waving hello at the start of a live video lesson"
            className={styles.heroImageMain}
          />
        </div>
      </PageContainer>
    </section>
  );
}
