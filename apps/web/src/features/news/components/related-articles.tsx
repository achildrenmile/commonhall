'use client';

import Image from 'next/image';
import Link from 'next/link';
import { Skeleton } from '@/components/ui/skeleton';
import { ScrollArea, ScrollBar } from '@/components/ui/scroll-area';
import { useRelatedArticles } from '../api';
import { formatRelativeTime } from '@/lib/utils';

interface RelatedArticlesProps {
  channelSlug: string;
  channelName: string;
  excludeArticleId: string;
}

export function RelatedArticles({
  channelSlug,
  channelName,
  excludeArticleId,
}: RelatedArticlesProps) {
  const { data, isLoading } = useRelatedArticles(channelSlug, excludeArticleId);

  if (isLoading) {
    return <RelatedArticlesSkeleton channelName={channelName} />;
  }

  if (!data?.items.length) {
    return null;
  }

  return (
    <section className="mt-12 pt-8 border-t border-slate-200 dark:border-slate-800">
      <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100 mb-4">
        More from {channelName}
      </h2>

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
                <div className="relative h-36 bg-gradient-to-br from-slate-100 to-slate-200 dark:from-slate-800 dark:to-slate-900">
                  {article.teaserImageUrl ? (
                    <Image
                      src={article.teaserImageUrl}
                      alt={article.title}
                      fill
                      className="object-cover transition-transform group-hover:scale-105"
                      sizes="288px"
                    />
                  ) : (
                    <div className="absolute inset-0 flex items-center justify-center">
                      <div className="text-3xl text-slate-300 dark:text-slate-600">📰</div>
                    </div>
                  )}
                </div>

                {/* Content */}
                <div className="p-4">
                  <h3 className="font-medium text-slate-900 dark:text-slate-100 line-clamp-2 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors mb-2">
                    {article.title}
                  </h3>
                  {article.teaserText && (
                    <p className="text-sm text-slate-500 line-clamp-2 mb-2">
                      {article.teaserText}
                    </p>
                  )}
                  <p className="text-xs text-slate-400">
                    {article.publishedAt ? formatRelativeTime(article.publishedAt) : ''}
                  </p>
                </div>
              </div>
            </Link>
          ))}
        </div>
        <ScrollBar orientation="horizontal" />
      </ScrollArea>
    </section>
  );
}

function RelatedArticlesSkeleton({ channelName }: { channelName: string }) {
  return (
    <section className="mt-12 pt-8 border-t border-slate-200 dark:border-slate-800">
      <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100 mb-4">
        More from {channelName}
      </h2>
      <div className="flex gap-4 overflow-hidden">
        {Array.from({ length: 3 }).map((_, i) => (
          <div
            key={i}
            className="shrink-0 w-72 rounded-lg border border-slate-200 dark:border-slate-800 overflow-hidden"
          >
            <Skeleton className="h-36 w-full" />
            <div className="p-4">
              <Skeleton className="h-5 w-full mb-2" />
              <Skeleton className="h-5 w-3/4 mb-2" />
              <Skeleton className="h-4 w-full mb-1" />
              <Skeleton className="h-4 w-2/3 mb-2" />
              <Skeleton className="h-3 w-20" />
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
