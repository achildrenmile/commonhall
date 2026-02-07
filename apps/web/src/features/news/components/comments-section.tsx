'use client';

import { useEffect, useRef, useCallback } from 'react';
import { MessageSquare, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { useComments } from '../api';
import { CommentComposer } from './comment-composer';
import { CommentItem } from './comment-item';

interface CommentsSectionProps {
  articleId: string;
  commentCount: number;
}

export function CommentsSection({ articleId, commentCount }: CommentsSectionProps) {
  const {
    data,
    isLoading,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
  } = useComments(articleId);

  const comments = data?.pages.flatMap((page) => page.items) || [];

  return (
    <section id="comments-section" className="mt-12 pt-8 border-t border-slate-200 dark:border-slate-800">
      {/* Header */}
      <div className="flex items-center gap-2 mb-6">
        <MessageSquare className="h-5 w-5 text-slate-500" />
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
          Comments ({commentCount})
        </h2>
      </div>

      {/* Composer */}
      <div className="mb-8">
        <CommentComposer articleId={articleId} />
      </div>

      {/* Comments List */}
      {isLoading ? (
        <CommentsSkeleton />
      ) : comments.length > 0 ? (
        <div className="space-y-6">
          {comments.map((comment) => (
            <CommentItem
              key={comment.id}
              comment={comment}
              articleId={articleId}
            />
          ))}
        </div>
      ) : (
        <div className="text-center py-8 text-slate-500">
          <p>No comments yet. Be the first to share your thoughts!</p>
        </div>
      )}

      {/* Load More */}
      {hasNextPage && (
        <div className="mt-6 flex justify-center">
          <Button
            variant="outline"
            onClick={() => fetchNextPage()}
            disabled={isFetchingNextPage}
          >
            {isFetchingNextPage ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Loading...
              </>
            ) : (
              'Load more comments'
            )}
          </Button>
        </div>
      )}
    </section>
  );
}

function CommentsSkeleton() {
  return (
    <div className="space-y-6">
      {Array.from({ length: 3 }).map((_, i) => (
        <div key={i} className="flex gap-3">
          <Skeleton className="h-8 w-8 rounded-full shrink-0" />
          <div className="flex-1">
            <div className="flex items-center gap-2 mb-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-3 w-16" />
            </div>
            <Skeleton className="h-4 w-full mb-1" />
            <Skeleton className="h-4 w-3/4" />
          </div>
        </div>
      ))}
    </div>
  );
}
