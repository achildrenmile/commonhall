'use client';

import { useState } from 'react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Loader2 } from 'lucide-react';
import { useAuthStore } from '@/lib/auth-store';
import { useAddComment } from '../api';

interface CommentComposerProps {
  articleId: string;
  parentId?: string;
  onSuccess?: () => void;
  onCancel?: () => void;
  placeholder?: string;
  autoFocus?: boolean;
}

export function CommentComposer({
  articleId,
  parentId,
  onSuccess,
  onCancel,
  placeholder = 'Write a comment...',
  autoFocus = false,
}: CommentComposerProps) {
  const [content, setContent] = useState('');
  const user = useAuthStore((state) => state.user);
  const addComment = useAddComment();

  const userInitials = user
    ? `${user.firstName?.[0] || ''}${user.lastName?.[0] || ''}`.toUpperCase()
    : '?';

  const handleSubmit = async () => {
    if (!content.trim()) return;

    try {
      await addComment.mutateAsync({
        articleId,
        content: content.trim(),
        parentId,
      });
      setContent('');
      onSuccess?.();
    } catch {
      // Error is handled by the mutation
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
      handleSubmit();
    }
  };

  if (!user) {
    return (
      <div className="rounded-lg border border-slate-200 dark:border-slate-800 p-4 text-center text-sm text-slate-500">
        Please sign in to leave a comment
      </div>
    );
  }

  return (
    <div className="flex gap-3">
      <Avatar className="h-8 w-8 shrink-0">
        <AvatarImage src={user.avatarUrl} alt={`${user.firstName} ${user.lastName}`} />
        <AvatarFallback>{userInitials}</AvatarFallback>
      </Avatar>
      <div className="flex-1 space-y-2">
        <Textarea
          value={content}
          onChange={(e) => setContent(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          autoFocus={autoFocus}
          className="min-h-[80px] resize-none"
        />
        <div className="flex items-center justify-between">
          <p className="text-xs text-slate-500">
            Press ⌘+Enter to submit
          </p>
          <div className="flex gap-2">
            {onCancel && (
              <Button variant="ghost" size="sm" onClick={onCancel}>
                Cancel
              </Button>
            )}
            <Button
              size="sm"
              onClick={handleSubmit}
              disabled={!content.trim() || addComment.isPending}
            >
              {addComment.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                'Post'
              )}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
