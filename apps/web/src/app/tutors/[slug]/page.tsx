import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { getTutorProfile } from '@/features/tutor-profile/api';
import { resolveMediaUrl } from '@/features/tutor-profile/mediaUrl';
import { TutorProfileView } from '@/features/tutor-profile/components/TutorProfileView';
import { ApiError } from '@/lib/api/client';
import { TutorPublicProfile } from '@/features/tutor-profile/types';

interface TutorProfilePageProps {
  params: Promise<{ slug: string }>;
}

async function loadProfile(slug: string): Promise<TutorPublicProfile | null> {
  try {
    return await getTutorProfile(slug);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return null;
    }
    throw error;
  }
}

export async function generateMetadata({ params }: TutorProfilePageProps): Promise<Metadata> {
  const { slug } = await params;
  const profile = await loadProfile(slug);

  if (!profile) {
    return {
      title: 'Tutor not found | TutorFlow',
      robots: { index: false, follow: false },
    };
  }

  const title = `${profile.displayName} — ${profile.headline ?? 'Tutor'} | TutorFlow`;
  const description = profile.about
    ? profile.about.slice(0, 160)
    : `${profile.displayName} is a tutor on TutorFlow.`;
  const photoUrl = resolveMediaUrl(profile.profilePhotoReference);

  return {
    title,
    description,
    alternates: { canonical: `/tutors/${profile.slug}` },
    openGraph: {
      title: profile.displayName,
      description,
      images: photoUrl ? [photoUrl] : undefined,
    },
  };
}

export default async function TutorProfilePage({ params }: TutorProfilePageProps) {
  const { slug } = await params;
  const profile = await loadProfile(slug);

  if (!profile) {
    notFound();
  }

  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': 'Person',
    name: profile.displayName,
    jobTitle: profile.headline ?? 'Tutor',
  };

  return (
    <>
      {/* Minimal Person structured data only — no AggregateRating until Reviews has real data
          (PREPLY_TUTOR_PROFILE_PARITY_SPEC.md §9, ADR-0022): emitting an empty/zero aggregate
          would be misleading structured data, not just an aesthetic choice. */}
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
      <TutorProfileView slug={slug} initialProfile={profile} />
    </>
  );
}
