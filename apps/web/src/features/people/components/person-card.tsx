'use client';

import Link from 'next/link';
import { Mail, Phone, MapPin, Building2 } from 'lucide-react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import type { Person } from '../api';

interface PersonCardProps {
  person: Person;
}

export function PersonCard({ person }: PersonCardProps) {
  const initials = `${person.firstName?.[0] || ''}${person.lastName?.[0] || ''}`.toUpperCase() ||
    person.displayName.substring(0, 2).toUpperCase();

  return (
    <Link href={`/people/${person.id}`} className="group block">
      <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 p-6 text-center hover:shadow-lg hover:border-slate-300 dark:hover:border-slate-700 hover:-translate-y-0.5 transition-all">
        {/* Avatar */}
        <Avatar className="h-20 w-20 mx-auto mb-4">
          <AvatarImage src={person.avatarUrl} alt={person.displayName} />
          <AvatarFallback className="text-xl bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300">
            {initials}
          </AvatarFallback>
        </Avatar>

        {/* Name */}
        <h3 className="font-semibold text-slate-900 dark:text-slate-100 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors truncate">
          {person.displayName}
        </h3>

        {/* Job Title */}
        {person.jobTitle && (
          <p className="text-sm text-slate-600 dark:text-slate-400 truncate mt-1">
            {person.jobTitle}
          </p>
        )}

        {/* Department & Location */}
        <div className="flex flex-col items-center gap-1 mt-3 text-xs text-slate-500">
          {person.department && (
            <div className="flex items-center gap-1">
              <Building2 className="h-3 w-3" />
              <span className="truncate">{person.department}</span>
            </div>
          )}
          {person.location && (
            <div className="flex items-center gap-1">
              <MapPin className="h-3 w-3" />
              <span className="truncate">{person.location}</span>
            </div>
          )}
        </div>

        {/* Quick Actions */}
        <div className="flex justify-center gap-2 mt-4">
          {person.email && (
            <Button
              variant="outline"
              size="sm"
              className="h-8 w-8 p-0"
              onClick={(e) => {
                e.preventDefault();
                window.location.href = `mailto:${person.email}`;
              }}
              title={`Email ${person.displayName}`}
            >
              <Mail className="h-4 w-4" />
            </Button>
          )}
          {person.phoneNumber && (
            <Button
              variant="outline"
              size="sm"
              className="h-8 w-8 p-0"
              onClick={(e) => {
                e.preventDefault();
                window.location.href = `tel:${person.phoneNumber}`;
              }}
              title={`Call ${person.displayName}`}
            >
              <Phone className="h-4 w-4" />
            </Button>
          )}
        </div>
      </div>
    </Link>
  );
}
