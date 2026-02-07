'use client';

import { useQuery } from '@tanstack/react-query';
import { Loader2, Mail, Building2, MapPin } from 'lucide-react';
import { apiClient } from '@/lib/api-client';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import type { WidgetProps, UserProfileData } from '../types';

interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  avatarUrl?: string;
  jobTitle?: string;
  department?: string;
  location?: string;
  bio?: string;
}

async function fetchUser(userId: string): Promise<UserProfile> {
  return apiClient.get<UserProfile>(`/users/${userId}`);
}

export default function UserProfileWidget({ data, id }: WidgetProps<UserProfileData>) {
  const { data: user, isLoading, error } = useQuery({
    queryKey: ['widget-user', id, data.userId],
    queryFn: () => fetchUser(data.userId),
    enabled: !!data.userId,
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
      </div>
    );
  }

  if (error || !user) {
    return (
      <div className="rounded-lg border border-slate-200 dark:border-slate-800 p-4 text-center text-sm text-slate-500">
        User not found
      </div>
    );
  }

  const initials = `${user.firstName?.[0] || ''}${user.lastName?.[0] || ''}`.toUpperCase();

  return (
    <div className="rounded-lg border border-slate-200 dark:border-slate-800 p-6">
      <div className="flex items-start gap-4">
        <Avatar className="h-16 w-16">
          <AvatarImage src={user.avatarUrl} alt={`${user.firstName} ${user.lastName}`} />
          <AvatarFallback className="text-lg">{initials}</AvatarFallback>
        </Avatar>
        <div className="flex-1 min-w-0">
          <h3 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
            {user.firstName} {user.lastName}
          </h3>
          {user.jobTitle && (
            <p className="text-sm text-slate-600 dark:text-slate-400">
              {user.jobTitle}
            </p>
          )}
          <div className="mt-3 space-y-1.5">
            {user.department && (
              <div className="flex items-center gap-2 text-sm text-slate-500">
                <Building2 className="h-4 w-4" />
                <span>{user.department}</span>
              </div>
            )}
            {user.location && (
              <div className="flex items-center gap-2 text-sm text-slate-500">
                <MapPin className="h-4 w-4" />
                <span>{user.location}</span>
              </div>
            )}
            <div className="flex items-center gap-2 text-sm text-slate-500">
              <Mail className="h-4 w-4" />
              <a href={`mailto:${user.email}`} className="hover:text-blue-600 dark:hover:text-blue-400">
                {user.email}
              </a>
            </div>
          </div>
          {user.bio && (
            <p className="mt-3 text-sm text-slate-600 dark:text-slate-400">
              {user.bio}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
