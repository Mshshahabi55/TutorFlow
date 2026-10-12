import Link from 'next/link';
import styles from './SiteFooter.module.css';
import { PageContainer } from './PageContainer';

const COLUMNS = [
  {
    heading: 'For Students',
    links: [
      { href: '/find-tutors', label: 'Find a Tutor' },
      { href: '/subjects', label: 'Browse Subjects' },
      { href: '/how-it-works', label: 'How It Works' },
    ],
  },
  {
    heading: 'For Tutors',
    links: [{ href: '/become-a-tutor', label: 'Become a Tutor' }],
  },
  {
    heading: 'Company',
    links: [
      { href: '/about', label: 'About TutorFlow' },
      { href: '/help', label: 'Help Center' },
    ],
  },
  {
    heading: 'Legal',
    links: [
      { href: '/terms', label: 'Terms of Service' },
      { href: '/privacy', label: 'Privacy Policy' },
    ],
  },
];

export function SiteFooter() {
  return (
    <footer className={styles.footer}>
      <PageContainer>
        <div className={styles.grid}>
          {COLUMNS.map((column) => (
            <div key={column.heading} className={styles.column}>
              <span className={styles.heading}>{column.heading}</span>
              {column.links.map((link) => (
                <Link key={link.href} href={link.href} className={styles.link}>
                  {link.label}
                </Link>
              ))}
            </div>
          ))}
        </div>
        <div className={styles.bottom}>© {new Date().getFullYear()} TutorFlow. All rights reserved.</div>
      </PageContainer>
    </footer>
  );
}
