'use client';

import Link from 'next/link';
import { Mail, Phone, MapPin, Building2 } from 'lucide-react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import type { Person } from '../api';

interface PersonRowProps {
  person: Person;
}

export function PersonRow({ person }: PersonRowProps) {
  const initials = `${person.firstName?.[0] || ''}${person.lastName?.[0] || ''}`.toUpperCase() ||
    person.displayName.substring(0, 2).toUpperCase();

  return (
    <Link
      href={`/people/${person.id}`}
      className="group flex items-center gap-4 p-4 rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 hover:shadow-md hover:border-slate-300 dark:hover:border-slate-700 transition-all"
    >
      {/* Avatar */}
      <Avatar className="h-10 w-10 shrink-0">
        <AvatarImage src={person.avatarUrl} alt={person.displayName} />
        <AvatarFallback className="text-sm bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300">
          {initials}
        </AvatarFallback>
      </Avatar>

      {/* Name & Title */}
      <div className="flex-1 min-w-0">
        <h3 className="font-medium text-slate-900 dark:text-slate-100 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors truncate">
          {person.displayName}
        </h3>
        {person.jobTitle && (
          <p className="text-sm text-slate-500 truncate">
            {person.jobTitle}
          </p>
        )}
      </div>

      {/* Department */}
      <div className="hidden sm:flex items-center gap-1.5 text-sm text-slate-500 min-w-0 w-40">
        <Building2 className="h-4 w-4 shrink-0" />
        <span className="truncate">{person.department || '—'}</span>
      </div>

      {/* Location */}
      <div className="hidden md:flex items-center gap-1.5 text-sm text-slate-500 min-w-0 w-36">
        <MapPin className="h-4 w-4 shrink-0" />
        <span className="truncate">{person.location || '—'}</span>
      </div>

      {/* Email */}
      <div className="hidden lg:block text-sm text-slate-500 min-w-0 w-56 truncate">
        {person.email && (
          <a
            href={`mailto:${person.email}`}
            onClick={(e) => e.stopPropagation()}
            className="flex items-center gap-1.5 hover:text-blue-600 dark:hover:text-blue-400"
          >
            <Mail className="h-4 w-4 shrink-0" />
            <span className="truncate">{person.email}</span>
          </a>
        )}
      </div>

      {/* Phone */}
      <div className="hidden xl:block text-sm text-slate-500 min-w-0 w-32">
        {person.phoneNumber && (
          <a
            href={`tel:${person.phoneNumber}`}
            onClick={(e) => e.stopPropagation()}
            className="flex items-center gap-1.5 hover:text-blue-600 dark:hover:text-blue-400"
          >
            <Phone className="h-4 w-4 shrink-0" />
            <span className="truncate">{person.phoneNumber}</span>
          </a>
        )}
      </div>
    </Link>
  );
}
