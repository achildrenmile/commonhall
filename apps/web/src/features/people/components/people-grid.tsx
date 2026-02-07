'use client';

import { useEffect, useRef, useCallback, useMemo } from 'react';
import { Loader2, Users } from 'lucide-react';
import { usePeopleSearch, type PeopleSearchFilters, type Person } from '../api';
import { PersonCard } from './person-card';
import { PersonRow } from './person-row';
import { PersonGridSkeleton, PersonListSkeleton } from './person-skeleton';

interface PeopleGridProps {
  filters: PeopleSearchFilters;
  viewMode: 'grid' | 'list';
}

// Group people by first letter of display name
function groupByLetter(people: Person[]): Map<string, Person[]> {
  const grouped = new Map<string, Person[]>();

  for (const person of people) {
    const letter = person.displayName.charAt(0).toUpperCase();
    const existing = grouped.get(letter) || [];
    grouped.set(letter, [...existing, person]);
  }

  return grouped;
}

export function PeopleGrid({ filters, viewMode }: PeopleGridProps) {
  const {
    data,
    isLoading,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
    error,
  } = usePeopleSearch(filters);

  const observerRef = useRef<IntersectionObserver | null>(null);
  const loadMoreRef = useRef<HTMLDivElement>(null);

  const handleObserver = useCallback(
    (entries: IntersectionObserverEntry[]) => {
      const [entry] = entries;
      if (entry.isIntersecting && hasNextPage && !isFetchingNextPage) {
        fetchNextPage();
      }
    },
    [fetchNextPage, hasNextPage, isFetchingNextPage]
  );

  useEffect(() => {
    const element = loadMoreRef.current;
    if (!element) return;

    observerRef.current = new IntersectionObserver(handleObserver, {
      root: null,
      rootMargin: '100px',
      threshold: 0,
    });

    observerRef.current.observe(element);

    return () => {
      if (observerRef.current) {
        observerRef.current.disconnect();
      }
    };
  }, [handleObserver]);

  const allPeople = useMemo(() => {
    return data?.pages.flatMap((page) => page.items) || [];
  }, [data]);

  const totalCount = data?.pages[0]?.totalCount || 0;

  // Determine if we're browsing (no search query) to show alphabetical headers
  const isBrowsing = !filters.query;
  const groupedPeople = useMemo(() => {
    if (!isBrowsing) return null;
    return groupByLetter(allPeople);
  }, [allPeople, isBrowsing]);

  if (isLoading) {
    return viewMode === 'grid' ? <PersonGridSkeleton /> : <PersonListSkeleton />;
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <p className="text-slate-500">Failed to load people. Please try again.</p>
      </div>
    );
  }

  if (allPeople.length === 0) {
    return (
      <div className="text-center py-12">
        <Users className="h-12 w-12 text-slate-300 dark:text-slate-600 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100 mb-2">
          No people found
        </h3>
        <p className="text-slate-500">
          {filters.query
            ? 'Try adjusting your search or filters'
            : 'No employees match the selected filters'}
        </p>
      </div>
    );
  }

  // Render with alphabetical headers when browsing
  if (isBrowsing && groupedPeople) {
    const sortedLetters = Array.from(groupedPeople.keys()).sort();

    return (
      <>
        {/* Results Count */}
        <p className="text-sm text-slate-500 mb-4">
          {totalCount.toLocaleString()} {totalCount === 1 ? 'person' : 'people'}
        </p>

        <div className="space-y-8">
          {sortedLetters.map((letter) => {
            const people = groupedPeople.get(letter)!;
            return (
              <div key={letter}>
                <h2 className="text-lg font-bold text-slate-900 dark:text-slate-100 mb-4 sticky top-16 bg-slate-50 dark:bg-slate-900 py-2 z-10">
                  {letter}
                </h2>
                {viewMode === 'grid' ? (
                  <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                    {people.map((person) => (
                      <PersonCard key={person.id} person={person} />
                    ))}
                  </div>
                ) : (
                  <div className="space-y-3">
                    {people.map((person) => (
                      <PersonRow key={person.id} person={person} />
                    ))}
                  </div>
                )}
              </div>
            );
          })}
        </div>

        {/* Load More Trigger */}
        <div ref={loadMoreRef} className="py-8 flex justify-center">
          {isFetchingNextPage && (
            <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
          )}
        </div>
      </>
    );
  }

  // Render search results without headers
  return (
    <>
      {/* Results Count */}
      <p className="text-sm text-slate-500 mb-4">
        {totalCount.toLocaleString()} {totalCount === 1 ? 'result' : 'results'}
      </p>

      {viewMode === 'grid' ? (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {allPeople.map((person) => (
            <PersonCard key={person.id} person={person} />
          ))}
        </div>
      ) : (
        <div className="space-y-3">
          {allPeople.map((person) => (
            <PersonRow key={person.id} person={person} />
          ))}
        </div>
      )}

      {/* Load More Trigger */}
      <div ref={loadMoreRef} className="py-8 flex justify-center">
        {isFetchingNextPage && (
          <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
        )}
      </div>
    </>
  );
}
