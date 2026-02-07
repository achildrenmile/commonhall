'use client';

import Link from 'next/link';
import { FileText, ChevronRight } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface PageItem {
  id: string;
  title: string;
  slug: string;
  icon?: string;
  description?: string;
}

interface SpacePagesListProps {
  pages: PageItem[];
  spaceSlug: string;
  title?: string;
}

export function SpacePagesList({ pages, spaceSlug, title = 'Pages' }: SpacePagesListProps) {
  if (pages.length === 0) {
    return null;
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">{title}</CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        <ul className="divide-y divide-slate-200 dark:divide-slate-800">
          {pages.map((page) => (
            <li key={page.id}>
              <Link
                href={`/spaces/${spaceSlug}/${page.slug}`}
                className="flex items-center gap-3 px-6 py-3 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors group"
              >
                <span className="text-lg">{page.icon || '📄'}</span>
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-slate-900 dark:text-slate-100 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                    {page.title}
                  </p>
                  {page.description && (
                    <p className="text-sm text-slate-500 truncate">
                      {page.description}
                    </p>
                  )}
                </div>
                <ChevronRight className="h-4 w-4 text-slate-400 group-hover:text-slate-600 dark:group-hover:text-slate-300 transition-colors" />
              </Link>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  );
}
