'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Plus, Search, Loader2, MessageSquare } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import { formatRelativeTime } from '@/lib/utils';
import { useConversations } from '../api';
import type { ConversationListItem } from '../types';

interface ConversationListProps {
  selectedId?: string;
  onSelect?: (conversationId: string) => void;
  onNewConversation?: () => void;
}

function ConversationItem({
  conversation,
  isSelected,
  onClick,
}: {
  conversation: ConversationListItem;
  isSelected: boolean;
  onClick: () => void;
}) {
  const displayName =
    conversation.type === 'Direct'
      ? conversation.otherParticipantName || 'Unknown'
      : conversation.name || 'Group Chat';

  const initials =
    conversation.type === 'Direct'
      ? (conversation.otherParticipantName || 'U')
          .split(' ')
          .map((n) => n[0])
          .join('')
          .slice(0, 2)
          .toUpperCase()
      : (conversation.name || 'GC').slice(0, 2).toUpperCase();

  const avatarUrl =
    conversation.type === 'Direct' ? conversation.otherParticipantProfilePhotoUrl : undefined;

  return (
    <button
      onClick={onClick}
      className={cn(
        'w-full flex items-start gap-3 p-3 rounded-lg text-left transition-colors',
        isSelected
          ? 'bg-slate-100 dark:bg-slate-800'
          : 'hover:bg-slate-50 dark:hover:bg-slate-800/50'
      )}
    >
      <Avatar className="h-10 w-10 shrink-0">
        <AvatarImage src={avatarUrl || undefined} />
        <AvatarFallback>{initials}</AvatarFallback>
      </Avatar>
      <div className="flex-1 min-w-0">
        <div className="flex items-center justify-between gap-2">
          <span className="font-medium truncate">{displayName}</span>
          {conversation.lastMessageAt && (
            <span className="text-xs text-muted-foreground shrink-0">
              {formatRelativeTime(conversation.lastMessageAt)}
            </span>
          )}
        </div>
        {conversation.lastMessagePreview && (
          <p className="text-sm text-muted-foreground truncate mt-0.5">
            {conversation.lastMessageAuthor && (
              <span className="font-medium">{conversation.lastMessageAuthor}: </span>
            )}
            {conversation.lastMessagePreview}
          </p>
        )}
      </div>
      {conversation.unreadCount > 0 && (
        <Badge variant="default" className="shrink-0">
          {conversation.unreadCount > 99 ? '99+' : conversation.unreadCount}
        </Badge>
      )}
    </button>
  );
}

export function ConversationList({
  selectedId,
  onSelect,
  onNewConversation,
}: ConversationListProps) {
  const [search, setSearch] = useState('');
  const { data: conversations, isLoading } = useConversations();
  const router = useRouter();

  const handleSelect = (id: string) => {
    onSelect?.(id);
    router.push(`/messages/${id}`);
  };

  const filteredConversations = conversations?.filter((conv) => {
    if (!search) return true;
    const searchLower = search.toLowerCase();
    const name =
      conv.type === 'Direct' ? conv.otherParticipantName : conv.name;
    return name?.toLowerCase().includes(searchLower);
  });

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="p-4 border-b">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold">Messages</h2>
          <Button size="sm" onClick={onNewConversation}>
            <Plus className="h-4 w-4 mr-1" />
            New
          </Button>
        </div>
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search conversations..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>
      </div>

      {/* Conversations List */}
      <div className="flex-1 overflow-y-auto p-2">
        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : !filteredConversations?.length ? (
          <div className="text-center py-8">
            <MessageSquare className="h-12 w-12 text-muted-foreground mx-auto mb-3" />
            <p className="text-sm text-muted-foreground">
              {search ? 'No conversations found' : 'No messages yet'}
            </p>
            {!search && (
              <Button variant="outline" size="sm" className="mt-4" onClick={onNewConversation}>
                <Plus className="h-4 w-4 mr-1" />
                Start a conversation
              </Button>
            )}
          </div>
        ) : (
          <div className="space-y-1">
            {filteredConversations.map((conv) => (
              <ConversationItem
                key={conv.id}
                conversation={conv}
                isSelected={conv.id === selectedId}
                onClick={() => handleSelect(conv.id)}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
