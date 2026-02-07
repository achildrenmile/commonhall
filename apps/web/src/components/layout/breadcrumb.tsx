'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ChevronRight, Home } from 'lucide-react';
import { cn } from '@/lib/utils';

const routeLabels: Record<string, string> = {
  news: 'News',
  spaces: 'Spaces',
  people: 'People',
  messages: 'Messages',
  studio: 'Studio',
  settings: 'Settings',
  profile: 'Profile',
};

export function Breadcrumb() {
  const pathname = usePathname();
  const segments = pathname.split('/').filter(Boolean);

  if (segments.length === 0) {
    return (
      <div className="flex items-center text-sm">
        <Home className="h-4 w-4 text-slate-500" />
        <span className="ml-2 font-medium text-slate-900 dark:text-slate-100">Home</span>
      </div>
    );
  }

  return (
    <nav className="flex items-center text-sm" aria-label="Breadcrumb">
      <Link
        href="/"
        className="text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
      >
        <Home className="h-4 w-4" />
        <span className="sr-only">Home</span>
      </Link>

      {segments.map((segment, index) => {
        const href = '/' + segments.slice(0, index + 1).join('/');
        const isLast = index === segments.length - 1;
        const label = routeLabels[segment] || decodeURIComponent(segment);

        return (
          <div key={segment} className="flex items-center">
            <ChevronRight className="h-4 w-4 mx-2 text-slate-400" />
            {isLast ? (
              <span className="font-medium text-slate-900 dark:text-slate-100">
                {label}
              </span>
            ) : (
              <Link
                href={href}
                className="text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
              >
                {label}
              </Link>
            )}
          </div>
        );
      })}
    </nav>
  );
}
