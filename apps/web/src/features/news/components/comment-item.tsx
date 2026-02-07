'use client';

import { useState } from 'react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { MoreHorizontal, Pencil, Trash2, Reply, Loader2, AlertTriangle } from 'lucide-react';
import { formatRelativeTime } from '@/lib/utils';
import { useAuthStore } from '@/lib/auth-store';
import { useUpdateComment, useDeleteComment, type NewsComment } from '../api';
import { CommentComposer } from './comment-composer';

interface CommentItemProps {
  comment: NewsComment;
  articleId: string;
  isReply?: boolean;
}

export function CommentItem({ comment, articleId, isReply = false }: CommentItemProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [isReplying, setIsReplying] = useState(false);
  const [editContent, setEditContent] = useState(comment.content);

  const user = useAuthStore((state) => state.user);
  const updateComment = useUpdateComment();
  const deleteComment = useDeleteComment();

  const isOwner = user?.id === comment.author.id;
  const authorName = comment.author.displayName || `${comment.author.firstName} ${comment.author.lastName}`;
  const authorInitials = `${comment.author.firstName?.[0] || ''}${comment.author.lastName?.[0] || ''}`.toUpperCase();

  const handleUpdate = async () => {
    if (!editContent.trim()) return;

    try {
      await updateComment.mutateAsync({
        articleId,
        commentId: comment.id,
        content: editContent.trim(),
      });
      setIsEditing(false);
    } catch {
      // Error handled by mutation
    }
  };

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to delete this comment?')) return;

    try {
      await deleteComment.mutateAsync({
        articleId,
        commentId: comment.id,
      });
    } catch {
      // Error handled by mutation
    }
  };

  // Moderated comment placeholder
  if (comment.isModerated) {
    return (
      <div className={`flex gap-3 ${isReply ? 'ml-11' : ''}`}>
        <div className="flex items-center gap-2 py-3 px-4 rounded-lg bg-slate-50 dark:bg-slate-900 text-slate-500">
          <AlertTriangle className="h-4 w-4" />
          <span className="text-sm">This comment has been removed by a moderator</span>
        </div>
      </div>
    );
  }

  return (
    <div className={isReply ? 'ml-11' : ''}>
      <div className="flex gap-3">
        <Avatar className="h-8 w-8 shrink-0">
          <AvatarImage src={comment.author.avatarUrl} alt={authorName} />
          <AvatarFallback>{authorInitials}</AvatarFallback>
        </Avatar>
        <div className="flex-1 min-w-0">
          {/* Header */}
          <div className="flex items-center gap-2 mb-1">
            <span className="font-medium text-sm text-slate-900 dark:text-slate-100">
              {authorName}
            </span>
            <span className="text-xs text-slate-500">
              {formatRelativeTime(comment.createdAt)}
              {comment.isEdited && ' (edited)'}
            </span>
          </div>

          {/* Content */}
          {isEditing ? (
            <div className="space-y-2">
              <Textarea
                value={editContent}
                onChange={(e) => setEditContent(e.target.value)}
                className="min-h-[60px] resize-none"
              />
              <div className="flex gap-2">
                <Button
                  size="sm"
                  onClick={handleUpdate}
                  disabled={!editContent.trim() || updateComment.isPending}
                >
                  {updateComment.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    'Save'
                  )}
                </Button>
                <Button variant="ghost" size="sm" onClick={() => setIsEditing(false)}>
                  Cancel
                </Button>
              </div>
            </div>
          ) : (
            <p className="text-sm text-slate-700 dark:text-slate-300 whitespace-pre-wrap">
              {comment.content}
            </p>
          )}

          {/* Actions */}
          {!isEditing && !isReply && (
            <div className="flex items-center gap-2 mt-2">
              {user && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-7 px-2 text-xs"
                  onClick={() => setIsReplying(!isReplying)}
                >
                  <Reply className="h-3 w-3 mr-1" />
                  Reply
                </Button>
              )}
              {isOwner && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="sm" className="h-7 w-7 p-0">
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="start">
                    <DropdownMenuItem onClick={() => setIsEditing(true)}>
                      <Pencil className="h-4 w-4 mr-2" />
                      Edit
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={handleDelete}
                      className="text-red-600 dark:text-red-400"
                    >
                      <Trash2 className="h-4 w-4 mr-2" />
                      Delete
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Reply Composer */}
      {isReplying && (
        <div className="ml-11 mt-3">
          <CommentComposer
            articleId={articleId}
            parentId={comment.id}
            placeholder={`Reply to ${authorName}...`}
            onSuccess={() => setIsReplying(false)}
            onCancel={() => setIsReplying(false)}
            autoFocus
          />
        </div>
      )}

      {/* Nested Replies (1 level only) */}
      {comment.replies && comment.replies.length > 0 && (
        <div className="mt-4 space-y-4">
          {comment.replies.map((reply) => (
            <CommentItem
              key={reply.id}
              comment={reply}
              articleId={articleId}
              isReply
            />
          ))}
        </div>
      )}
    </div>
  );
}
