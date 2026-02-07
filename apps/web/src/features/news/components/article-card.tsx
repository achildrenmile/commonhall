'use client';

import Image from 'next/image';
import Link from 'next/link';
import { MessageSquare, Heart, Pin } from 'lucide-react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { formatRelativeTime } from '@/lib/utils';
import type { NewsArticle } from '../api';

interface ArticleCardProps {
  article: NewsArticle;
}

export function ArticleCard({ article }: ArticleCardProps) {
  const displayAuthor = article.displayAuthor || article.author;
  const authorInitials = `${displayAuthor.firstName?.[0] || ''}${displayAuthor.lastName?.[0] || ''}`.toUpperCase();
  const authorName = displayAuthor.displayName || `${displayAuthor.firstName} ${displayAuthor.lastName}`;

  return (
    <Link href={`/news/${article.slug}`} className="group block">
      <article className="h-full rounded-lg border border-slate-200 dark:border-slate-800 overflow-hidden bg-white dark:bg-slate-950 transition-all duration-200 hover:shadow-lg hover:border-slate-300 dark:hover:border-slate-700 hover:-translate-y-0.5">
        {/* Teaser Image */}
        <div className="relative aspect-video bg-gradient-to-br from-slate-100 to-slate-200 dark:from-slate-800 dark:to-slate-900 overflow-hidden">
          {article.teaserImageUrl ? (
            <Image
              src={article.teaserImageUrl}
              alt={article.title}
              fill
              className="object-cover transition-transform duration-300 group-hover:scale-105"
              sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw"
              loading="lazy"
            />
          ) : (
            <div className="absolute inset-0 flex items-center justify-center">
              <div className="text-4xl text-slate-300 dark:text-slate-600">📰</div>
            </div>
          )}

          {/* Channel Badge */}
          {article.channel && (
            <div className="absolute top-3 left-3">
              <Badge
                variant="secondary"
                className="bg-white/90 dark:bg-slate-900/90 backdrop-blur-sm"
                style={article.channel.color ? { borderLeftColor: article.channel.color, borderLeftWidth: 3 } : undefined}
              >
                {article.channel.name}
              </Badge>
            </div>
          )}

          {/* Pinned Indicator */}
          {article.isPinned && (
            <div className="absolute top-3 right-3">
              <Badge variant="default" className="gap-1">
                <Pin className="h-3 w-3" />
                Pinned
              </Badge>
            </div>
          )}
        </div>

        {/* Content */}
        <div className="p-4">
          {/* Title */}
          <h3 className="font-semibold text-slate-900 dark:text-slate-100 line-clamp-2 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors mb-2">
            {article.title}
          </h3>

          {/* Teaser Text */}
          {article.teaserText && (
            <p className="text-sm text-slate-600 dark:text-slate-400 line-clamp-3 mb-4">
              {article.teaserText}
            </p>
          )}

          {/* Footer */}
          <div className="flex items-center justify-between pt-3 border-t border-slate-100 dark:border-slate-800">
            {/* Author */}
            <div className="flex items-center gap-2 min-w-0">
              <Avatar className="h-6 w-6">
                <AvatarImage src={displayAuthor.avatarUrl} alt={authorName} />
                <AvatarFallback className="text-xs">{authorInitials}</AvatarFallback>
              </Avatar>
              <div className="min-w-0">
                <p className="text-xs font-medium text-slate-700 dark:text-slate-300 truncate">
                  {authorName}
                </p>
                <p className="text-xs text-slate-500">
                  {article.publishedAt ? formatRelativeTime(article.publishedAt) : 'Draft'}
                </p>
              </div>
            </div>

            {/* Stats */}
            <div className="flex items-center gap-3 text-slate-500">
              <span className="flex items-center gap-1 text-xs">
                <MessageSquare className="h-3.5 w-3.5" />
                {article.commentCount}
              </span>
              <span className="flex items-center gap-1 text-xs">
                <Heart className="h-3.5 w-3.5" />
                {article.likeCount}
              </span>
            </div>
          </div>
        </div>
      </article>
    </Link>
  );
}
