'use client';

import { useParams } from 'next/navigation';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { useUserProfile } from '@/features/people';
import {
  ProfileCard,
  ProfileInfo,
  ProfileArticles,
  ProfileSkeleton,
} from '@/features/people';

export default function PersonProfilePage() {
  const params = useParams<{ id: string }>();
  const { data: profile, isLoading, error } = useUserProfile(params.id);

  if (isLoading) {
    return <ProfileSkeleton />;
  }

  if (error || !profile) {
    return (
      <div className="max-w-4xl mx-auto">
        <Button variant="ghost" asChild className="mb-6">
          <Link href="/people">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to directory
          </Link>
        </Button>

        <div className="flex flex-col items-center justify-center min-h-[300px] text-center">
          <div className="text-6xl mb-4">👤</div>
          <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100 mb-2">
            Person not found
          </h2>
          <p className="text-slate-500">
            The person you're looking for doesn't exist or has been removed.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto">
      {/* Back Button */}
      <Button variant="ghost" asChild className="mb-6">
        <Link href="/people">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to directory
        </Link>
      </Button>

      <div className="space-y-8">
        {/* Profile Card with Cover */}
        <ProfileCard profile={profile} />

        {/* Info Sections */}
        <ProfileInfo profile={profile} />

        {/* Recent Articles */}
        <ProfileArticles articles={profile.recentArticles} />
      </div>
    </div>
  );
}
