import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { TutorProfileHero } from './TutorProfileHero';
import { TutorAbout } from './TutorAbout';
import { TutorCredentials } from './TutorCredentials';
import { TutorTrustBadges } from './TutorTrustBadges';
import { TutorSubjectsAndSpecialties } from './TutorSubjectsAndSpecialties';
import { TutorLanguages } from './TutorLanguages';
import { AuthGateModal } from '@/components/auth/AuthGateModal';
import { TutorPublicProfile } from '../types';

vi.mock('@/features/catalog/useCatalog', () => ({
  useSubjects: () => ({ data: [{ id: 's1', code: 'english', displayName: 'English' }], isLoading: false }),
  useSpecialties: () => ({ data: [], isLoading: false }),
  useLanguages: () => ({ data: [{ code: 'en', displayName: 'English' }], isLoading: false }),
}));

function renderWithProviders(ui: ReactElement) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

const sampleProfile: TutorPublicProfile = {
  tutorUserId: '61d4dd3a-08c0-4494-b452-55a3c33aac50',
  slug: 'jane-dev-verify01',
  displayName: 'Jane Dev',
  headline: 'Friendly conversational English tutor',
  about: 'I help students build confidence speaking English.',
  profilePhotoReference: null,
  introVideoReference: null,
  spokenLanguages: [{ languageCode: 'en', proficiency: 'Native' }],
  subjectIds: ['s1'],
  specialtyIds: [],
  countryOfBirthCode: 'GB',
  isProfessionalTutor: true,
  isSuperTutor: true,
  yearsOfExperience: 5,
  experienceSummary: 'Five years teaching online.',
  educationEntries: [{ institution: 'University of Leeds', degree: 'BA Linguistics', fieldOfStudy: null, startYear: 2014, endYear: 2017 }],
  certificateEntries: [{ name: 'TEFL', issuingOrganization: 'ITA', issueYear: 2018, fileReference: null }],
  hourlyRateAmountMinorUnits: 2500,
  hourlyRateCurrency: 'USD',
  isAcceptingNewStudents: true,
};

describe('TutorProfileHero', () => {
  it('renders name, headline, and both trust badges', () => {
    renderWithProviders(<TutorProfileHero profile={sampleProfile} />);
    expect(screen.getByRole('heading', { name: 'Jane Dev' })).toBeInTheDocument();
    expect(screen.getByText('Friendly conversational English tutor')).toBeInTheDocument();
    expect(screen.getByText('Super Tutor')).toBeInTheDocument();
    expect(screen.getByText('Professional Tutor')).toBeInTheDocument();
    expect(screen.getByText('No reviews yet')).toBeInTheDocument();
  });

  it('has no detectable axe violations', async () => {
    const { container } = renderWithProviders(<TutorProfileHero profile={sampleProfile} />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

describe('TutorTrustBadges', () => {
  it('renders nothing when neither badge applies', () => {
    const { container } = render(<TutorTrustBadges isSuperTutor={false} isProfessionalTutor={false} />);
    expect(container).toBeEmptyDOMElement();
  });
});

describe('TutorAbout', () => {
  it('renders the about text when present', () => {
    render(<TutorAbout about="Hello there" />);
    expect(screen.getByText('Hello there')).toBeInTheDocument();
  });

  it('renders nothing when about is null — matches the omit-when-absent precedent', () => {
    const { container } = render(<TutorAbout about={null} />);
    expect(container).toBeEmptyDOMElement();
  });
});

describe('TutorCredentials', () => {
  it('renders experience, education, and certificates', () => {
    render(
      <TutorCredentials
        yearsOfExperience={sampleProfile.yearsOfExperience}
        experienceSummary={sampleProfile.experienceSummary}
        educationEntries={sampleProfile.educationEntries}
        certificateEntries={sampleProfile.certificateEntries}
      />,
    );
    expect(screen.getByText(/5 years of teaching experience/)).toBeInTheDocument();
    expect(screen.getByText(/BA Linguistics/)).toBeInTheDocument();
    expect(screen.getByText('TEFL')).toBeInTheDocument();
  });

  it('renders nothing when there is no credential data at all', () => {
    const { container } = render(
      <TutorCredentials yearsOfExperience={null} experienceSummary={null} educationEntries={[]} certificateEntries={[]} />,
    );
    expect(container).toBeEmptyDOMElement();
  });
});

describe('TutorSubjectsAndSpecialties', () => {
  it('resolves subject IDs to display names via the catalog', () => {
    render(<TutorSubjectsAndSpecialties subjectIds={['s1']} specialtyIds={[]} />);
    expect(screen.getByText('English')).toBeInTheDocument();
  });
});

describe('TutorLanguages', () => {
  it('resolves language codes to display names via the catalog', () => {
    render(<TutorLanguages spokenLanguages={[{ languageCode: 'en', proficiency: 'Native' }]} />);
    expect(screen.getByText('English')).toBeInTheDocument();
    expect(screen.getByText('Native')).toBeInTheDocument();
  });
});

describe('AuthGateModal', () => {
  it('describes the gated action and links to login/signup', () => {
    render(<AuthGateModal open onOpenChange={vi.fn()} action="book a trial lesson" />);
    expect(screen.getByText(/book a trial lesson/)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute('href', '/login');
    expect(screen.getByRole('link', { name: 'Sign up' })).toHaveAttribute('href', '/signup');
  });

  it('carries returnTo through to both login and signup links so the user lands back where they started', () => {
    render(<AuthGateModal open onOpenChange={vi.fn()} action="book a trial lesson" returnTo="/tutors/jane-dev" />);
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute(
      'href',
      '/login?returnTo=%2Ftutors%2Fjane-dev',
    );
    expect(screen.getByRole('link', { name: 'Sign up' })).toHaveAttribute(
      'href',
      '/signup?returnTo=%2Ftutors%2Fjane-dev',
    );
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(<AuthGateModal open onOpenChange={vi.fn()} action="favorite this tutor" />);
    expect((await axe(container)).violations).toEqual([]);
  });
});
