'use client';

import { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { PinnedArticles, NewsFilters, NewsFeedGrid } from '@/features/news';
import { ArticleCardSkeletonGrid } from '@/features/news';
import type { NewsFilters as NewsFiltersType } from '@/features/news';

function NewsContent() {
  const searchParams = useSearchParams();

  const filters: NewsFiltersType = {
    channelSlug: searchParams.get('channel') || undefined,
    spaceSlug: searchParams.get('space') || undefined,
    tagSlug: searchParams.get('tag') || undefined,
    sort: (searchParams.get('sort') as 'latest' | 'popular') || 'latest',
  };

  return (
    <>
      <PinnedArticles spaceSlug={filters.spaceSlug} />
      <NewsFilters />
      <NewsFeedGrid filters={filters} />
    </>
  );
}

export default function NewsPage() {
  return (
    <div className="max-w-7xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          News
        </h1>
        <p className="text-slate-600 dark:text-slate-400">
          Stay up to date with the latest announcements and updates
        </p>
      </div>

      <Suspense fallback={<ArticleCardSkeletonGrid count={6} />}>
        <NewsContent />
      </Suspense>
    </div>
  );
}
