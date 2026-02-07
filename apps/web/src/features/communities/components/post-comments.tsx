'use client';

import { useState } from 'react';
import { Loader2, Send } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { useToast } from '@/hooks/use-toast';
import { formatRelativeTime } from '@/lib/utils';
import { usePostComments, useAddComment } from '../api';

interface PostCommentsProps {
  slug: string;
  postId: string;
}

export function PostComments({ slug, postId }: PostCommentsProps) {
  const [body, setBody] = useState('');
  const { data: comments, isLoading } = usePostComments(slug, postId);
  const addCommentMutation = useAddComment();
  const { toast } = useToast();

  const handleSubmit = async () => {
    if (!body.trim()) return;

    try {
      await addCommentMutation.mutateAsync({ slug, postId, body });
      setBody('');
    } catch {
      toast({ title: 'Error', description: 'Failed to add comment', variant: 'destructive' });
    }
  };

  return (
    <div className="space-y-4">
      {/* Comment Form */}
      <div className="flex gap-3">
        <Textarea
          placeholder="Write a comment..."
          value={body}
          onChange={(e) => setBody(e.target.value)}
          rows={2}
          className="flex-1"
        />
        <Button
          size="sm"
          onClick={handleSubmit}
          disabled={!body.trim() || addCommentMutation.isPending}
        >
          {addCommentMutation.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Send className="h-4 w-4" />
          )}
        </Button>
      </div>

      {/* Comments List */}
      {isLoading ? (
        <div className="flex justify-center py-4">
          <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
        </div>
      ) : !comments?.length ? (
        <p className="text-sm text-muted-foreground text-center py-2">
          No comments yet. Be the first to comment!
        </p>
      ) : (
        <div className="space-y-3">
          {comments.map((comment) => {
            const name = [comment.authorFirstName, comment.authorLastName]
              .filter(Boolean)
              .join(' ') || 'Unknown';
            const initials = `${comment.authorFirstName?.[0] || ''}${comment.authorLastName?.[0] || ''}`;

            return (
              <div key={comment.id} className="flex gap-3">
                <Avatar className="h-8 w-8">
                  <AvatarImage src={comment.authorProfilePhotoUrl || undefined} />
                  <AvatarFallback className="text-xs">{initials || '?'}</AvatarFallback>
                </Avatar>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-0.5">
                    <span className="text-sm font-medium">{name}</span>
                    <span className="text-xs text-muted-foreground">
                      {formatRelativeTime(comment.createdAt)}
                    </span>
                  </div>
                  <p className="text-sm whitespace-pre-wrap break-words">{comment.body}</p>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
