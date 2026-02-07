'use client';

import { Mail, Phone, MapPin, Building2 } from 'lucide-react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import type { PersonProfile } from '../api';

interface ProfileCardProps {
  profile: PersonProfile;
}

export function ProfileCard({ profile }: ProfileCardProps) {
  const initials = `${profile.firstName?.[0] || ''}${profile.lastName?.[0] || ''}`.toUpperCase() ||
    profile.displayName.substring(0, 2).toUpperCase();

  return (
    <div className="relative">
      {/* Cover Gradient */}
      <div className="h-32 sm:h-40 bg-gradient-to-br from-blue-500 via-purple-500 to-pink-500 rounded-t-xl" />

      {/* Profile Content */}
      <div className="bg-white dark:bg-slate-950 rounded-b-xl border border-t-0 border-slate-200 dark:border-slate-800 px-6 pb-6">
        {/* Avatar - Positioned to overlap cover */}
        <div className="flex flex-col sm:flex-row sm:items-end gap-4 -mt-16 sm:-mt-12">
          <Avatar className="h-28 w-28 sm:h-32 sm:w-32 border-4 border-white dark:border-slate-950 shadow-lg">
            <AvatarImage src={profile.avatarUrl} alt={profile.displayName} />
            <AvatarFallback className="text-3xl bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300">
              {initials}
            </AvatarFallback>
          </Avatar>

          <div className="flex-1 pb-2">
            <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 dark:text-slate-100">
              {profile.displayName}
            </h1>
            {profile.jobTitle && (
              <p className="text-lg text-slate-600 dark:text-slate-400">
                {profile.jobTitle}
              </p>
            )}
          </div>
        </div>

        {/* Department & Location */}
        <div className="flex flex-wrap gap-4 mt-4 text-sm text-slate-600 dark:text-slate-400">
          {profile.department && (
            <div className="flex items-center gap-1.5">
              <Building2 className="h-4 w-4" />
              <span>{profile.department}</span>
            </div>
          )}
          {profile.location && (
            <div className="flex items-center gap-1.5">
              <MapPin className="h-4 w-4" />
              <span>{profile.location}</span>
            </div>
          )}
        </div>

        {/* Bio */}
        {profile.bio && (
          <p className="mt-4 text-slate-700 dark:text-slate-300">
            {profile.bio}
          </p>
        )}

        {/* Contact Buttons */}
        <div className="flex flex-wrap gap-3 mt-6">
          {profile.email && (
            <Button asChild>
              <a href={`mailto:${profile.email}`}>
                <Mail className="h-4 w-4 mr-2" />
                Email
              </a>
            </Button>
          )}
          {profile.phoneNumber && (
            <Button variant="outline" asChild>
              <a href={`tel:${profile.phoneNumber}`}>
                <Phone className="h-4 w-4 mr-2" />
                Call
              </a>
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
