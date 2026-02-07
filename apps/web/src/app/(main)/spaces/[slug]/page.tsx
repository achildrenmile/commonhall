'use client';

import { useParams } from 'next/navigation';
import { Loader2 } from 'lucide-react';
import { useSpace } from '@/features/spaces';
import { SpaceLanding } from '@/features/spaces';

export default function SpacePage() {
  const params = useParams<{ slug: string }>();
  const { data: space, isLoading, error } = useSpace(params.slug);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin text-slate-400" />
      </div>
    );
  }

  if (error || !space) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[400px] text-center">
        <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100 mb-2">
          Space not found
        </h2>
        <p className="text-slate-500">
          The space you're looking for doesn't exist or you don't have access to it.
        </p>
      </div>
    );
  }

  return <SpaceLanding space={space} />;
}
