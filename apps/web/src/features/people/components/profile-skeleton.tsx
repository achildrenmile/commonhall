'use client';

import { Skeleton } from '@/components/ui/skeleton';
import { Card, CardContent, CardHeader } from '@/components/ui/card';

export function ProfileSkeleton() {
  return (
    <div className="max-w-4xl mx-auto space-y-8">
      {/* Profile Card Skeleton */}
      <div>
        <Skeleton className="h-32 sm:h-40 rounded-t-xl" />
        <div className="bg-white dark:bg-slate-950 rounded-b-xl border border-t-0 border-slate-200 dark:border-slate-800 px-6 pb-6">
          <div className="flex flex-col sm:flex-row sm:items-end gap-4 -mt-16 sm:-mt-12">
            <Skeleton className="h-28 w-28 sm:h-32 sm:w-32 rounded-full" />
            <div className="flex-1 pb-2">
              <Skeleton className="h-8 w-48 mb-2" />
              <Skeleton className="h-5 w-32" />
            </div>
          </div>
          <div className="flex gap-4 mt-4">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-4 w-28" />
          </div>
          <Skeleton className="h-16 w-full mt-4" />
          <div className="flex gap-3 mt-6">
            <Skeleton className="h-10 w-24" />
            <Skeleton className="h-10 w-20" />
          </div>
        </div>
      </div>

      {/* Info Cards Skeleton */}
      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <Skeleton className="h-5 w-40" />
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-start gap-3">
              <Skeleton className="h-5 w-5 rounded" />
              <div className="flex-1">
                <Skeleton className="h-3 w-12 mb-1" />
                <Skeleton className="h-4 w-48" />
              </div>
            </div>
            <div className="flex items-start gap-3">
              <Skeleton className="h-5 w-5 rounded" />
              <div className="flex-1">
                <Skeleton className="h-3 w-12 mb-1" />
                <Skeleton className="h-4 w-32" />
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <Skeleton className="h-5 w-44" />
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-start gap-3">
              <Skeleton className="h-5 w-5 rounded" />
              <div className="flex-1">
                <Skeleton className="h-3 w-8 mb-1" />
                <Skeleton className="h-4 w-20" />
              </div>
            </div>
            <div className="flex items-start gap-3">
              <Skeleton className="h-5 w-5 rounded" />
              <div className="flex-1">
                <Skeleton className="h-3 w-12 mb-1" />
                <Skeleton className="h-4 w-28" />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
