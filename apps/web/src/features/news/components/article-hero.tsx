'use client';

import Image from 'next/image';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Clock, Eye, Calendar } from 'lucide-react';
import { formatDate } from '@/lib/utils';
import type { NewsArticle } from '../api';

interface ArticleHeroProps {
  article: NewsArticle;
}

export function ArticleHero({ article }: ArticleHeroProps) {
  const displayAuthor = article.displayAuthor || article.author;
  const authorName = displayAuthor.displayName || `${displayAuthor.firstName} ${displayAuthor.lastName}`;
  const authorInitials = `${displayAuthor.firstName?.[0] || ''}${displayAuthor.lastName?.[0] || ''}`.toUpperCase();

  return (
    <div className="mb-8">
      {/* Hero Image */}
      {article.teaserImageUrl && (
        <div className="relative w-full h-64 sm:h-80 lg:h-96 rounded-xl overflow-hidden mb-6">
          <Image
            src={article.teaserImageUrl}
            alt={article.title}
            fill
            className="object-cover"
            priority
            sizes="(max-width: 1200px) 100vw, 1200px"
          />
        </div>
      )}

      {/* Channel Badge */}
      {article.channel && (
        <Badge
          variant="secondary"
          className="mb-4"
          style={article.channel.color ? { borderLeftColor: article.channel.color, borderLeftWidth: 3 } : undefined}
        >
          {article.channel.name}
        </Badge>
      )}

      {/* Title */}
      <h1 className="text-3xl sm:text-4xl font-bold text-slate-900 dark:text-slate-100 mb-4">
        {article.title}
      </h1>

      {/* Meta Row */}
      <div className="flex flex-wrap items-center gap-4 text-sm text-slate-600 dark:text-slate-400 mb-4">
        {/* Author */}
        <div className="flex items-center gap-2">
          <Avatar className="h-8 w-8">
            <AvatarImage src={displayAuthor.avatarUrl} alt={authorName} />
            <AvatarFallback>{authorInitials}</AvatarFallback>
          </Avatar>
          <span className="font-medium text-slate-900 dark:text-slate-100">
            {authorName}
          </span>
        </div>

        {/* Date */}
        {article.publishedAt && (
          <div className="flex items-center gap-1.5">
            <Calendar className="h-4 w-4" />
            <span>{formatDate(article.publishedAt)}</span>
          </div>
        )}

        {/* Reading Time */}
        {article.readingTimeMinutes && (
          <div className="flex items-center gap-1.5">
            <Clock className="h-4 w-4" />
            <span>{article.readingTimeMinutes} min read</span>
          </div>
        )}

        {/* Views */}
        <div className="flex items-center gap-1.5">
          <Eye className="h-4 w-4" />
          <span>{article.viewCount.toLocaleString()} views</span>
        </div>
      </div>

      {/* Tags */}
      {article.tags.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {article.tags.map((tag) => (
            <Badge key={tag.id} variant="outline">
              {tag.name}
            </Badge>
          ))}
        </div>
      )}
    </div>
  );
}
