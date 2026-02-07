'use client';

import { Skeleton } from '@/components/ui/skeleton';

export function PersonCardSkeleton() {
  return (
    <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 p-6">
      <Skeleton className="h-20 w-20 rounded-full mx-auto mb-4" />
      <Skeleton className="h-5 w-32 mx-auto mb-2" />
      <Skeleton className="h-4 w-24 mx-auto mb-3" />
      <div className="flex flex-col items-center gap-1">
        <Skeleton className="h-3 w-28" />
        <Skeleton className="h-3 w-20" />
      </div>
      <div className="flex justify-center gap-2 mt-4">
        <Skeleton className="h-8 w-8 rounded-md" />
        <Skeleton className="h-8 w-8 rounded-md" />
      </div>
    </div>
  );
}

export function PersonRowSkeleton() {
  return (
    <div className="flex items-center gap-4 p-4 rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950">
      <Skeleton className="h-10 w-10 rounded-full shrink-0" />
      <div className="flex-1 min-w-0">
        <Skeleton className="h-5 w-40 mb-1" />
        <Skeleton className="h-4 w-32" />
      </div>
      <Skeleton className="hidden sm:block h-4 w-32" />
      <Skeleton className="hidden md:block h-4 w-28" />
      <Skeleton className="hidden lg:block h-4 w-48" />
      <Skeleton className="hidden xl:block h-4 w-28" />
    </div>
  );
}

export function PersonGridSkeleton({ count = 12 }: { count?: number }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {Array.from({ length: count }).map((_, i) => (
        <PersonCardSkeleton key={i} />
      ))}
    </div>
  );
}

export function PersonListSkeleton({ count = 8 }: { count?: number }) {
  return (
    <div className="space-y-3">
      {Array.from({ length: count }).map((_, i) => (
        <PersonRowSkeleton key={i} />
      ))}
    </div>
  );
}
