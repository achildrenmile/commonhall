'use client';

import { use, useState } from 'react';
import { useRouter } from 'next/navigation';
import { MessageSquare } from 'lucide-react';
import { ConversationList, MessageThread, NewConversationDialog } from '@/features/messages';

interface ConversationPageProps {
  params: Promise<{ conversationId: string }>;
}

export default function ConversationPage({ params }: ConversationPageProps) {
  const { conversationId } = use(params);
  const [showNewConversation, setShowNewConversation] = useState(false);
  const router = useRouter();

  return (
    <div className="h-[calc(100vh-4rem)] flex">
      {/* Conversations List - Hidden on mobile when viewing a conversation */}
      <div className="hidden lg:flex w-80 border-r flex-col bg-card">
        <ConversationList
          selectedId={conversationId}
          onNewConversation={() => setShowNewConversation(true)}
        />
      </div>

      {/* Message Thread - Full width on mobile, right panel on desktop */}
      <div className="flex-1 flex flex-col bg-card">
        <MessageThread
          conversationId={conversationId}
          onBack={() => router.push('/messages')}
        />
      </div>

      <NewConversationDialog
        open={showNewConversation}
        onOpenChange={setShowNewConversation}
      />
    </div>
  );
}
