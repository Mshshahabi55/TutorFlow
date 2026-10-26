import { ConversationThreadView } from './ConversationThreadView';

interface PageProps {
  params: Promise<{ conversationId: string }>;
}

export const metadata = { title: 'Conversation | TutorFlow' };

export default async function ConversationThreadPage({ params }: PageProps) {
  const { conversationId } = await params;
  return <ConversationThreadView conversationId={conversationId} />;
}
