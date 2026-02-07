'use client';

import { useState } from 'react';
import { MessageSquare } from 'lucide-react';
import { ConversationList, NewConversationDialog } from '@/features/messages';

export default function MessagesPage() {
  const [showNewConversation, setShowNewConversation] = useState(false);

  return (
    <div className="h-[calc(100vh-4rem)] flex">
      {/* Conversations List - Full width on mobile, left panel on desktop */}
      <div className="w-full lg:w-80 lg:border-r flex flex-col bg-card">
        <ConversationList onNewConversation={() => setShowNewConversation(true)} />
      </div>

      {/* Empty state for desktop - hidden on mobile */}
      <div className="hidden lg:flex flex-1 items-center justify-center bg-muted/30">
        <div className="text-center">
          <MessageSquare className="h-16 w-16 text-muted-foreground mx-auto mb-4" />
          <h3 className="text-lg font-semibold mb-2">Select a conversation</h3>
          <p className="text-sm text-muted-foreground">
            Choose a conversation from the list to start chatting
          </p>
        </div>
      </div>

      <NewConversationDialog
        open={showNewConversation}
        onOpenChange={setShowNewConversation}
      />
    </div>
  );
}
