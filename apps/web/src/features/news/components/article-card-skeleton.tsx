'use client';

import { Skeleton } from '@/components/ui/skeleton';

export function ArticleCardSkeleton() {
  return (
    <div className="rounded-lg border border-slate-200 dark:border-slate-800 overflow-hidden bg-white dark:bg-slate-950">
      {/* Image Skeleton */}
      <Skeleton className="aspect-video w-full" />

      {/* Content */}
      <div className="p-4">
        {/* Title */}
        <Skeleton className="h-5 w-4/5 mb-2" />
        <Skeleton className="h-5 w-2/3 mb-3" />

        {/* Teaser Text */}
        <Skeleton className="h-4 w-full mb-1" />
        <Skeleton className="h-4 w-full mb-1" />
        <Skeleton className="h-4 w-3/4 mb-4" />

        {/* Footer */}
        <div className="flex items-center justify-between pt-3 border-t border-slate-100 dark:border-slate-800">
          <div className="flex items-center gap-2">
            <Skeleton className="h-6 w-6 rounded-full" />
            <div>
              <Skeleton className="h-3 w-20 mb-1" />
              <Skeleton className="h-3 w-14" />
            </div>
          </div>
          <div className="flex items-center gap-3">
            <Skeleton className="h-4 w-8" />
            <Skeleton className="h-4 w-8" />
          </div>
        </div>
      </div>
    </div>
  );
}

export function ArticleCardSkeletonGrid({ count = 6 }: { count?: number }) {
  return (
    <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: count }).map((_, i) => (
        <ArticleCardSkeleton key={i} />
      ))}
    </div>
  );
}
