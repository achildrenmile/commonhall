'use client';

import { useState } from 'react';
import { Heart, MessageSquare, Share2, Check, Link2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { useToast } from '@/components/ui/use-toast';
import { cn } from '@/lib/utils';
import { useToggleReaction } from '../api';

interface ArticleInteractionBarProps {
  articleId: string;
  articleSlug: string;
  likeCount: number;
  commentCount: number;
  hasLiked: boolean;
}

export function ArticleInteractionBar({
  articleId,
  articleSlug,
  likeCount,
  commentCount,
  hasLiked,
}: ArticleInteractionBarProps) {
  const { toast } = useToast();
  const [copied, setCopied] = useState(false);
  const toggleReaction = useToggleReaction(articleSlug);

  const handleLike = () => {
    toggleReaction.mutate();
  };

  const handleCopyLink = async () => {
    const url = `${window.location.origin}/news/${articleSlug}`;
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      toast({
        title: 'Link copied',
        description: 'Article link has been copied to clipboard',
      });
      setTimeout(() => setCopied(false), 2000);
    } catch {
      toast({
        title: 'Failed to copy',
        description: 'Could not copy link to clipboard',
        variant: 'destructive',
      });
    }
  };

  const scrollToComments = () => {
    const commentsSection = document.getElementById('comments-section');
    if (commentsSection) {
      commentsSection.scrollIntoView({ behavior: 'smooth' });
    }
  };

  return (
    <div className="sticky bottom-4 z-10 flex justify-center">
      <div className="inline-flex items-center gap-1 p-1 rounded-full bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 shadow-lg">
        {/* Like Button */}
        <Button
          variant="ghost"
          size="sm"
          onClick={handleLike}
          disabled={toggleReaction.isPending}
          className={cn(
            'gap-2 rounded-full',
            hasLiked && 'text-red-500 hover:text-red-600'
          )}
        >
          <Heart
            className={cn('h-4 w-4', hasLiked && 'fill-current')}
          />
          <span>{likeCount}</span>
        </Button>

        {/* Comment Button */}
        <Button
          variant="ghost"
          size="sm"
          onClick={scrollToComments}
          className="gap-2 rounded-full"
        >
          <MessageSquare className="h-4 w-4" />
          <span>{commentCount}</span>
        </Button>

        {/* Share Button */}
        <Popover>
          <PopoverTrigger asChild>
            <Button variant="ghost" size="sm" className="rounded-full">
              <Share2 className="h-4 w-4" />
            </Button>
          </PopoverTrigger>
          <PopoverContent className="w-48 p-2" align="center">
            <Button
              variant="ghost"
              size="sm"
              className="w-full justify-start gap-2"
              onClick={handleCopyLink}
            >
              {copied ? (
                <>
                  <Check className="h-4 w-4 text-green-500" />
                  Copied!
                </>
              ) : (
                <>
                  <Link2 className="h-4 w-4" />
                  Copy link
                </>
              )}
            </Button>
          </PopoverContent>
        </Popover>
      </div>
    </div>
  );
}
