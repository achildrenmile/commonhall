'use client';

import { useEffect, useRef, useCallback } from 'react';
import { Loader2 } from 'lucide-react';
import { useNewsFeed, type NewsFilters } from '../api';
import { ArticleCard } from './article-card';
import { ArticleCardSkeletonGrid } from './article-card-skeleton';

interface NewsFeedGridProps {
  filters: NewsFilters;
}

export function NewsFeedGrid({ filters }: NewsFeedGridProps) {
  const {
    data,
    isLoading,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
    error,
  } = useNewsFeed(filters);

  const observerRef = useRef<IntersectionObserver | null>(null);
  const loadMoreRef = useRef<HTMLDivElement>(null);

  const handleObserver = useCallback(
    (entries: IntersectionObserverEntry[]) => {
      const [entry] = entries;
      if (entry.isIntersecting && hasNextPage && !isFetchingNextPage) {
        fetchNextPage();
      }
    },
    [fetchNextPage, hasNextPage, isFetchingNextPage]
  );

  useEffect(() => {
    const element = loadMoreRef.current;
    if (!element) return;

    observerRef.current = new IntersectionObserver(handleObserver, {
      root: null,
      rootMargin: '100px',
      threshold: 0,
    });

    observerRef.current.observe(element);

    return () => {
      if (observerRef.current) {
        observerRef.current.disconnect();
      }
    };
  }, [handleObserver]);

  if (isLoading) {
    return <ArticleCardSkeletonGrid count={6} />;
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <p className="text-slate-500">Failed to load articles. Please try again.</p>
      </div>
    );
  }

  const articles = data?.pages.flatMap((page) => page.items) || [];

  if (articles.length === 0) {
    return (
      <div className="text-center py-12">
        <div className="text-4xl mb-4">📰</div>
        <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100 mb-2">
          No articles found
        </h3>
        <p className="text-slate-500">
          Try adjusting your filters or check back later for new content.
        </p>
      </div>
    );
  }

  return (
    <>
      <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        {articles.map((article) => (
          <ArticleCard key={article.id} article={article} />
        ))}
      </div>

      {/* Load More Trigger */}
      <div ref={loadMoreRef} className="py-8 flex justify-center">
        {isFetchingNextPage && (
          <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
        )}
      </div>
    </>
  );
}
