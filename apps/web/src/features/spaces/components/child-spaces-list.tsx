'use client';

import Link from 'next/link';
import Image from 'next/image';
import { Folder, ChevronRight } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface ChildSpace {
  id: string;
  name: string;
  slug: string;
  description?: string;
  iconUrl?: string;
}

interface ChildSpacesListProps {
  spaces: ChildSpace[];
  title?: string;
}

export function ChildSpacesList({ spaces, title = 'Child Spaces' }: ChildSpacesListProps) {
  if (spaces.length === 0) {
    return null;
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">{title}</CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        <ul className="divide-y divide-slate-200 dark:divide-slate-800">
          {spaces.map((space) => (
            <li key={space.id}>
              <Link
                href={`/spaces/${space.slug}`}
                className="flex items-center gap-3 px-6 py-3 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors group"
              >
                {space.iconUrl ? (
                  <div className="relative h-8 w-8 rounded overflow-hidden shrink-0">
                    <Image
                      src={space.iconUrl}
                      alt={space.name}
                      fill
                      className="object-cover"
                    />
                  </div>
                ) : (
                  <Folder className="h-5 w-5 text-slate-400 shrink-0" />
                )}
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-slate-900 dark:text-slate-100 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                    {space.name}
                  </p>
                  {space.description && (
                    <p className="text-sm text-slate-500 truncate">
                      {space.description}
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
