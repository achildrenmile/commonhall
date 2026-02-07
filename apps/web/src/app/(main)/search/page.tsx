'use client';

import { Suspense } from 'react';
import { SearchPage } from '@/features/search';

export default function Search() {
  return (
    <Suspense fallback={<div className="text-center py-12">Loading...</div>}>
      <SearchPage />
    </Suspense>
  );
}
