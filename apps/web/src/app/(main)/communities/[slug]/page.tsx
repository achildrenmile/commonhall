'use client';

import { use } from 'react';
import { CommunityDetail } from '@/features/communities';

interface CommunityPageProps {
  params: Promise<{ slug: string }>;
}

export default function CommunityPage({ params }: CommunityPageProps) {
  const { slug } = use(params);

  return (
    <div className="max-w-4xl mx-auto">
      <CommunityDetail slug={slug} />
    </div>
  );
}
