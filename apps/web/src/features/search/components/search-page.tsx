'use client';

import { useState, useEffect } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import {
  Search,
  Loader2,
  FileText,
  Newspaper,
  User,
  File,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { cn } from '@/lib/utils';
import { formatRelativeTime } from '@/lib/utils';
import { useDebounce } from '@/lib/hooks/use-debounce';
import { useSearch } from '../api';
import { trackSearch } from '@/lib/analytics';
import type { SearchHit, SearchFilters } from '../types';

const typeIcons: Record<string, React.ElementType> = {
  news: Newspaper,
  pages: FileText,
  users: User,
  files: File,
};

const typeLabels: Record<string, string> = {
  news: 'News Articles',
  pages: 'Pages',
  users: 'People',
  files: 'Files',
};

const PAGE_SIZE = 20;

function SearchResultItem({ hit }: { hit: SearchHit }) {
  const Icon = typeIcons[hit.type] || FileText;

  return (
    <a
      href={hit.url}
      className="block p-4 hover:bg-muted/50 rounded-lg transition-colors"
    >
      <div className="flex gap-4">
        {hit.type === 'users' ? (
          <Avatar className="h-12 w-12 shrink-0">
            <AvatarImage src={hit.imageUrl || undefined} />
            <AvatarFallback>
              {hit.title.split(' ').map(n => n[0]).join('').slice(0, 2)}
            </AvatarFallback>
          </Avatar>
        ) : hit.imageUrl ? (
          <img
            src={hit.imageUrl}
            alt=""
            className="h-12 w-12 rounded object-cover shrink-0"
          />
        ) : (
          <div className="h-12 w-12 rounded bg-muted flex items-center justify-center shrink-0">
            <Icon className="h-6 w-6 text-muted-foreground" />
          </div>
        )}
        <div className="flex-1 min-w-0">
          <div className="flex items-start gap-2 mb-1">
            <h3
              className="font-medium text-foreground"
              dangerouslySetInnerHTML={{
                __html: hit.highlightedTitle || hit.title,
              }}
            />
            <span className="text-xs bg-muted px-2 py-0.5 rounded-full text-muted-foreground shrink-0">
              {typeLabels[hit.type]?.replace(/s$/, '') || hit.type}
            </span>
          </div>
          {(hit.highlightedExcerpt || hit.excerpt) && (
            <p
              className="text-sm text-muted-foreground line-clamp-2 mb-1"
              dangerouslySetInnerHTML={{
                __html: hit.highlightedExcerpt || hit.excerpt || '',
              }}
            />
          )}
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            {hit.subtitle && <span>{hit.subtitle}</span>}
            {hit.date && (
              <>
                {hit.subtitle && <span>•</span>}
                <span>{formatRelativeTime(hit.date)}</span>
              </>
            )}
          </div>
        </div>
      </div>
    </a>
  );
}

export function SearchPage() {
  const searchParams = useSearchParams();
  const router = useRouter();

  const initialQuery = searchParams.get('q') || '';
  const initialType = searchParams.get('type') || undefined;
  const initialSpace = searchParams.get('space') || undefined;
  const initialPage = parseInt(searchParams.get('page') || '1', 10);

  const [inputValue, setInputValue] = useState(initialQuery);
  const [selectedTypes, setSelectedTypes] = useState<string[]>(
    initialType ? [initialType] : []
  );
  const [selectedSpace, setSelectedSpace] = useState<string | undefined>(initialSpace);
  const [page, setPage] = useState(initialPage);

  const debouncedQuery = useDebounce(inputValue, 300);

  const filters: SearchFilters = {
    query: debouncedQuery,
    type: selectedTypes.length === 1 ? selectedTypes[0] : undefined,
    space: selectedSpace,
    from: (page - 1) * PAGE_SIZE,
    size: PAGE_SIZE,
  };

  const { data: result, isLoading, isFetching } = useSearch(filters);

  // Track search
  useEffect(() => {
    if (debouncedQuery.length >= 2 && result) {
      trackSearch(debouncedQuery, result.total);
    }
  }, [debouncedQuery, result]);

  // Update URL when filters change
  useEffect(() => {
    const params = new URLSearchParams();
    if (debouncedQuery) params.set('q', debouncedQuery);
    if (selectedTypes.length === 1) params.set('type', selectedTypes[0]);
    if (selectedSpace) params.set('space', selectedSpace);
    if (page > 1) params.set('page', String(page));

    const newUrl = `/search${params.toString() ? `?${params.toString()}` : ''}`;
    router.replace(newUrl, { scroll: false });
  }, [debouncedQuery, selectedTypes, selectedSpace, page, router]);

  const handleTypeToggle = (type: string) => {
    setSelectedTypes(prev =>
      prev.includes(type)
        ? prev.filter(t => t !== type)
        : [...prev, type]
    );
    setPage(1);
  };

  const handleSpaceSelect = (space: string | undefined) => {
    setSelectedSpace(space);
    setPage(1);
  };

  const totalPages = result ? Math.ceil(result.total / PAGE_SIZE) : 0;
  const showLoading = isLoading || isFetching;

  return (
    <div className="max-w-6xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold mb-4">Search</h1>
        <div className="relative">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Search news, pages, people, files..."
            value={inputValue}
            onChange={(e) => {
              setInputValue(e.target.value);
              setPage(1);
            }}
            className="pl-12 h-12 text-lg"
          />
        </div>
      </div>

      <div className="flex gap-8">
        {/* Filters Sidebar */}
        <aside className="w-64 shrink-0 hidden lg:block">
          <div className="sticky top-4 space-y-6">
            {/* Type Filters */}
            <div>
              <h3 className="font-medium mb-3">Type</h3>
              <div className="space-y-2">
                {Object.entries(typeLabels).map(([type, label]) => {
                  const count = result?.typeFacets[type] || 0;
                  const Icon = typeIcons[type];
                  const isSelected = selectedTypes.includes(type);

                  return (
                    <label
                      key={type}
                      className={cn(
                        'flex items-center gap-3 p-2 rounded-lg cursor-pointer transition-colors',
                        isSelected ? 'bg-primary/10' : 'hover:bg-muted'
                      )}
                    >
                      <Checkbox
                        checked={isSelected}
                        onCheckedChange={() => handleTypeToggle(type)}
                      />
                      <Icon className="h-4 w-4 text-muted-foreground" />
                      <span className="flex-1">{label}</span>
                      <span className="text-sm text-muted-foreground">{count}</span>
                    </label>
                  );
                })}
              </div>
            </div>

            {/* Space Filters */}
            {result && Object.keys(result.spaceFacets).length > 0 && (
              <div>
                <h3 className="font-medium mb-3">Space</h3>
                <div className="space-y-1">
                  <button
                    onClick={() => handleSpaceSelect(undefined)}
                    className={cn(
                      'w-full text-left px-3 py-2 rounded-lg text-sm transition-colors',
                      !selectedSpace ? 'bg-primary/10 font-medium' : 'hover:bg-muted'
                    )}
                  >
                    All spaces
                  </button>
                  {Object.entries(result.spaceFacets)
                    .sort((a, b) => b[1] - a[1])
                    .slice(0, 10)
                    .map(([space, count]) => (
                      <button
                        key={space}
                        onClick={() => handleSpaceSelect(space)}
                        className={cn(
                          'w-full flex items-center justify-between px-3 py-2 rounded-lg text-sm transition-colors',
                          selectedSpace === space ? 'bg-primary/10 font-medium' : 'hover:bg-muted'
                        )}
                      >
                        <span className="truncate">{space}</span>
                        <span className="text-muted-foreground">{count}</span>
                      </button>
                    ))}
                </div>
              </div>
            )}
          </div>
        </aside>

        {/* Results */}
        <main className="flex-1 min-w-0">
          {inputValue.length < 2 ? (
            <div className="text-center py-12 text-muted-foreground">
              <Search className="h-12 w-12 mx-auto mb-4" />
              <p>Enter at least 2 characters to search</p>
            </div>
          ) : showLoading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
          ) : !result || result.hits.length === 0 ? (
            <div className="text-center py-12">
              <Search className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
              <p className="text-lg font-medium mb-1">No results found</p>
              <p className="text-muted-foreground">
                Try different keywords or remove filters
              </p>
            </div>
          ) : (
            <>
              <div className="flex items-center justify-between mb-4">
                <p className="text-sm text-muted-foreground">
                  {result.total.toLocaleString()} result{result.total !== 1 ? 's' : ''}
                </p>
              </div>

              <div className="space-y-2">
                {result.hits.map((hit) => (
                  <SearchResultItem key={`${hit.type}-${hit.id}`} hit={hit} />
                ))}
              </div>

              {/* Pagination */}
              {totalPages > 1 && (
                <div className="flex items-center justify-center gap-2 mt-8">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPage(p => Math.max(1, p - 1))}
                    disabled={page === 1}
                  >
                    <ChevronLeft className="h-4 w-4 mr-1" />
                    Previous
                  </Button>
                  <span className="text-sm text-muted-foreground px-4">
                    Page {page} of {totalPages}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                    disabled={page === totalPages}
                  >
                    Next
                    <ChevronRight className="h-4 w-4 ml-1" />
                  </Button>
                </div>
              )}
            </>
          )}
        </main>
      </div>
    </div>
  );
}
