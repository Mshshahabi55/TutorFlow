import { AdminTutorApplicationDetailView } from './AdminTutorApplicationDetailView';

interface PageProps {
  params: Promise<{ id: string }>;
}

export const metadata = { title: 'Tutor application | TutorFlow Admin' };

export default async function AdminTutorApplicationDetailPage({ params }: PageProps) {
  const { id } = await params;
  return <AdminTutorApplicationDetailView id={id} />;
}
