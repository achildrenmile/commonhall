'use client';

import { useState, useEffect, useRef, useMemo, useCallback } from 'react';
import { ArrowLeft, Loader2, Send, MoreHorizontal, Pencil, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { cn } from '@/lib/utils';
import { formatRelativeTime } from '@/lib/utils';
import { useAuthStore } from '@/lib/auth-store';
import { useConversation, useMessages, useSendMessage, useMarkAsRead } from '../api';
import { useChatHub } from '../hooks/use-chat-hub';
import type { Message } from '../types';

interface MessageThreadProps {
  conversationId: string;
  onBack?: () => void;
}

interface TypingIndicator {
  userId: string;
  name: string;
}

function MessageBubble({
  message,
  isOwn,
  showAvatar,
}: {
  message: Message;
  isOwn: boolean;
  showAvatar: boolean;
}) {
  const authorName = [message.authorFirstName, message.authorLastName]
    .filter(Boolean)
    .join(' ') || 'Unknown';
  const initials = `${message.authorFirstName?.[0] || ''}${message.authorLastName?.[0] || ''}`;

  if (message.isDeleted) {
    return (
      <div className={cn('flex gap-2 mb-2', isOwn && 'flex-row-reverse')}>
        {showAvatar && !isOwn ? (
          <Avatar className="h-8 w-8 shrink-0">
            <AvatarImage src={message.authorProfilePhotoUrl || undefined} />
            <AvatarFallback className="text-xs">{initials || '?'}</AvatarFallback>
          </Avatar>
        ) : (
          <div className="w-8 shrink-0" />
        )}
        <div className="max-w-[70%] px-3 py-2 rounded-lg bg-muted text-muted-foreground italic text-sm">
          This message was deleted
        </div>
      </div>
    );
  }

  return (
    <div className={cn('flex gap-2 mb-2 group', isOwn && 'flex-row-reverse')}>
      {showAvatar && !isOwn ? (
        <Avatar className="h-8 w-8 shrink-0">
          <AvatarImage src={message.authorProfilePhotoUrl || undefined} />
          <AvatarFallback className="text-xs">{initials || '?'}</AvatarFallback>
        </Avatar>
      ) : (
        <div className="w-8 shrink-0" />
      )}
      <div className={cn('max-w-[70%]', isOwn && 'text-right')}>
        {showAvatar && !isOwn && (
          <span className="text-xs font-medium text-muted-foreground mb-1 block">
            {authorName}
          </span>
        )}
        <div
          className={cn(
            'px-3 py-2 rounded-lg inline-block text-left',
            isOwn
              ? 'bg-primary text-primary-foreground rounded-br-sm'
              : 'bg-muted rounded-bl-sm'
          )}
        >
          <p className="whitespace-pre-wrap break-words text-sm">{message.body}</p>
        </div>
        <div className="flex items-center gap-1 mt-1 text-xs text-muted-foreground">
          <span>{formatRelativeTime(message.createdAt)}</span>
          {message.editedAt && <span>(edited)</span>}
        </div>
      </div>
    </div>
  );
}

export function MessageThread({ conversationId, onBack }: MessageThreadProps) {
  const [messageBody, setMessageBody] = useState('');
  const [typingUsers, setTypingUsers] = useState<TypingIndicator[]>([]);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const isTypingRef = useRef(false);

  const user = useAuthStore((state) => state.user);
  const { data: conversation, isLoading: conversationLoading } = useConversation(conversationId);
  const { data: messagesData, isLoading: messagesLoading, fetchNextPage, hasNextPage, isFetchingNextPage } = useMessages(conversationId);
  const sendMutation = useSendMessage();
  const markAsReadMutation = useMarkAsRead();

  const messages = useMemo(() => {
    return messagesData?.pages.flatMap((page) => page.items).reverse() || [];
  }, [messagesData]);

  // Handle typing events
  const handleUserTyping = useCallback((convId: string, userId: string, isTyping: boolean) => {
    if (convId !== conversationId || userId === user?.id) return;

    setTypingUsers((prev) => {
      if (isTyping) {
        const participant = conversation?.participants.find((p) => p.userId === userId);
        const name = [participant?.firstName, participant?.lastName].filter(Boolean).join(' ') || 'Someone';
        if (!prev.find((t) => t.userId === userId)) {
          return [...prev, { userId, name }];
        }
        return prev;
      } else {
        return prev.filter((t) => t.userId !== userId);
      }
    });
  }, [conversationId, user?.id, conversation?.participants]);

  // SignalR hook
  const { isConnected, sendMessage, startTyping, stopTyping, markAsRead, joinConversation } = useChatHub({
    onUserTyping: handleUserTyping,
  });

  // Join conversation on mount
  useEffect(() => {
    if (isConnected && conversationId) {
      joinConversation(conversationId);
    }
  }, [isConnected, conversationId, joinConversation]);

  // Mark as read when viewing
  useEffect(() => {
    if (conversationId) {
      markAsReadMutation.mutate(conversationId);
      if (isConnected) {
        markAsRead(conversationId);
      }
    }
  }, [conversationId, isConnected, markAsRead, markAsReadMutation]);

  // Scroll to bottom on new messages
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages.length]);

  // Handle typing indicator
  const handleInputChange = (value: string) => {
    setMessageBody(value);

    if (!isTypingRef.current && value.trim()) {
      isTypingRef.current = true;
      startTyping(conversationId);
    }

    // Clear previous timeout
    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }

    // Set timeout to stop typing
    typingTimeoutRef.current = setTimeout(() => {
      if (isTypingRef.current) {
        isTypingRef.current = false;
        stopTyping(conversationId);
      }
    }, 2000);
  };

  const handleSend = async () => {
    if (!messageBody.trim()) return;

    const body = messageBody.trim();
    setMessageBody('');

    // Stop typing indicator
    if (isTypingRef.current) {
      isTypingRef.current = false;
      stopTyping(conversationId);
    }

    try {
      // Send via REST API (this will also broadcast via SignalR in the backend)
      await sendMutation.mutateAsync({ conversationId, body });
    } catch {
      // Restore message on error
      setMessageBody(body);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  // Get conversation display name
  const conversationName = useMemo(() => {
    if (!conversation) return 'Loading...';
    if (conversation.type === 'Direct') {
      const other = conversation.participants.find((p) => p.userId !== user?.id);
      return [other?.firstName, other?.lastName].filter(Boolean).join(' ') || 'Unknown';
    }
    return conversation.name || 'Group Chat';
  }, [conversation, user?.id]);

  const isLoading = conversationLoading || messagesLoading;

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center gap-3 p-4 border-b">
        {onBack && (
          <Button variant="ghost" size="icon" onClick={onBack} className="lg:hidden">
            <ArrowLeft className="h-5 w-5" />
          </Button>
        )}
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold truncate">{conversationName}</h3>
          {conversation && conversation.type !== 'Direct' && (
            <p className="text-sm text-muted-foreground">
              {conversation.participants.length} members
            </p>
          )}
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon">
              <MoreHorizontal className="h-5 w-5" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem>View details</DropdownMenuItem>
            <DropdownMenuItem>Mute notifications</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4">
        {isLoading ? (
          <div className="flex items-center justify-center h-full">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <>
            {/* Load more button */}
            {hasNextPage && (
              <div className="text-center mb-4">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => fetchNextPage()}
                  disabled={isFetchingNextPage}
                >
                  {isFetchingNextPage ? 'Loading...' : 'Load older messages'}
                </Button>
              </div>
            )}

            {/* Message bubbles */}
            {messages.map((message, index) => {
              const prevMessage = messages[index - 1];
              const showAvatar =
                !prevMessage || prevMessage.authorId !== message.authorId;

              return (
                <MessageBubble
                  key={message.id}
                  message={message}
                  isOwn={message.authorId === user?.id}
                  showAvatar={showAvatar}
                />
              );
            })}

            {/* Typing indicators */}
            {typingUsers.length > 0 && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground py-2">
                <div className="flex gap-1">
                  <span className="animate-bounce" style={{ animationDelay: '0ms' }}>•</span>
                  <span className="animate-bounce" style={{ animationDelay: '150ms' }}>•</span>
                  <span className="animate-bounce" style={{ animationDelay: '300ms' }}>•</span>
                </div>
                <span>
                  {typingUsers.length === 1
                    ? `${typingUsers[0].name} is typing...`
                    : `${typingUsers.length} people are typing...`}
                </span>
              </div>
            )}

            <div ref={messagesEndRef} />
          </>
        )}
      </div>

      {/* Message Input */}
      <div className="p-4 border-t">
        <div className="flex gap-2">
          <Textarea
            placeholder="Type a message..."
            value={messageBody}
            onChange={(e) => handleInputChange(e.target.value)}
            onKeyDown={handleKeyDown}
            rows={1}
            className="min-h-[40px] max-h-32 resize-none"
          />
          <Button onClick={handleSend} disabled={!messageBody.trim() || sendMutation.isPending}>
            {sendMutation.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Send className="h-4 w-4" />
            )}
          </Button>
        </div>
      </div>
    </div>
  );
}
