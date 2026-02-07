'use client';

import Image from 'next/image';
import Link from 'next/link';
import { Pin } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { ScrollArea, ScrollBar } from '@/components/ui/scroll-area';
import { usePinnedArticles } from '../api';
import { formatRelativeTime } from '@/lib/utils';

interface PinnedArticlesProps {
  spaceSlug?: string;
}

export function PinnedArticles({ spaceSlug }: PinnedArticlesProps) {
  const { data, isLoading } = usePinnedArticles(spaceSlug);

  if (isLoading) {
    return <PinnedArticlesSkeleton />;
  }

  if (!data?.items.length) {
    return null;
  }

  return (
    <div className="mb-8">
      <div className="flex items-center gap-2 mb-4">
        <Pin className="h-4 w-4 text-slate-500" />
        <h2 className="text-sm font-semibold text-slate-700 dark:text-slate-300 uppercase tracking-wide">
          Pinned
        </h2>
      </div>

      <ScrollArea className="w-full">
        <div className="flex gap-4 pb-4">
          {data.items.map((article) => (
            <Link
              key={article.id}
              href={`/news/${article.slug}`}
              className="group shrink-0 w-72"
            >
              <div className="rounded-lg border border-slate-200 dark:border-slate-800 overflow-hidden bg-white dark:bg-slate-950 hover:shadow-md hover:border-slate-300 dark:hover:border-slate-700 transition-all">
                {/* Image */}
                <div className="relative h-32 bg-gradient-to-br from-slate-100 to-slate-200 dark:from-slate-800 dark:to-slate-900">
                  {article.teaserImageUrl ? (
                    <Image
                      src={article.teaserImageUrl}
                      alt={article.title}
                      fill
                      className="object-cover"
                      sizes="288px"
                    />
                  ) : null}
                  <div className="absolute top-2 right-2">
                    <Badge variant="default" className="gap-1 text-xs">
                      <Pin className="h-3 w-3" />
                    </Badge>
                  </div>
                </div>

                {/* Content */}
                <div className="p-3">
                  {article.channel && (
                    <Badge variant="outline" className="text-xs mb-2">
                      {article.channel.name}
                    </Badge>
                  )}
                  <h3 className="font-medium text-sm text-slate-900 dark:text-slate-100 line-clamp-2 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                    {article.title}
                  </h3>
                  <p className="text-xs text-slate-500 mt-1">
                    {article.publishedAt ? formatRelativeTime(article.publishedAt) : ''}
                  </p>
                </div>
              </div>
            </Link>
          ))}
        </div>
        <ScrollBar orientation="horizontal" />
      </ScrollArea>
    </div>
  );
}

function PinnedArticlesSkeleton() {
  return (
    <div className="mb-8">
      <div className="flex items-center gap-2 mb-4">
        <Skeleton className="h-4 w-4" />
        <Skeleton className="h-4 w-16" />
      </div>
      <div className="flex gap-4 overflow-hidden">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="shrink-0 w-72 rounded-lg border border-slate-200 dark:border-slate-800 overflow-hidden">
            <Skeleton className="h-32 w-full" />
            <div className="p-3">
              <Skeleton className="h-4 w-16 mb-2" />
              <Skeleton className="h-4 w-full mb-1" />
              <Skeleton className="h-4 w-3/4 mb-2" />
              <Skeleton className="h-3 w-20" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
