'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { Loader2, Search, FileText, Newspaper, User, File, ArrowRight, Bot, Sparkles } from 'lucide-react';
import {
  CommandDialog,
  CommandInput,
  CommandList,
  CommandEmpty,
  CommandGroup,
  CommandItem,
  CommandSeparator,
} from '@/components/ui/command';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { useDebounce } from '@/lib/hooks/use-debounce';
import { useSearchSuggestions } from '../api';
import { trackSearch } from '@/lib/analytics';
import type { SearchSuggestion } from '../types';

const typeIcons: Record<string, React.ElementType> = {
  news: Newspaper,
  pages: FileText,
  users: User,
  files: File,
};

const typeLabels: Record<string, string> = {
  news: 'News',
  pages: 'Pages',
  users: 'People',
  files: 'Files',
};

interface QuickSearchProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function QuickSearch({ open, onOpenChange }: QuickSearchProps) {
  const [query, setQuery] = useState('');
  const debouncedQuery = useDebounce(query, 200);
  const router = useRouter();

  const { data: suggestions, isLoading, isFetching } = useSearchSuggestions(debouncedQuery);

  // Group suggestions by type
  const groupedSuggestions = suggestions?.reduce((acc, suggestion) => {
    const type = suggestion.type;
    if (!acc[type]) {
      acc[type] = [];
    }
    acc[type].push(suggestion);
    return acc;
  }, {} as Record<string, SearchSuggestion[]>) ?? {};

  const handleSelect = useCallback((suggestion: SearchSuggestion) => {
    trackSearch(query, suggestions?.length ?? 0, true);
    onOpenChange(false);
    setQuery('');
    router.push(suggestion.url);
  }, [query, suggestions, onOpenChange, router]);

  const handleViewAll = useCallback(() => {
    if (query.trim()) {
      trackSearch(query, suggestions?.length ?? 0, true);
      onOpenChange(false);
      router.push(`/search?q=${encodeURIComponent(query.trim())}`);
      setQuery('');
    }
  }, [query, suggestions, onOpenChange, router]);

  // Reset query when dialog closes
  useEffect(() => {
    if (!open) {
      setQuery('');
    }
  }, [open]);

  const showLoading = isLoading || isFetching;
  const hasResults = Object.keys(groupedSuggestions).length > 0;

  return (
    <CommandDialog open={open} onOpenChange={onOpenChange}>
      <CommandInput
        placeholder="Search news, pages, people, files..."
        value={query}
        onValueChange={setQuery}
      />
      <CommandList>
        {query.length < 2 ? (
          <CommandEmpty>
            <div className="flex flex-col items-center py-6 text-muted-foreground">
              <Search className="h-8 w-8 mb-2" />
              <p>Type to search...</p>
            </div>
          </CommandEmpty>
        ) : showLoading ? (
          <div className="flex items-center justify-center py-6">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : !hasResults ? (
          <CommandEmpty>No results found for "{query}"</CommandEmpty>
        ) : (
          <>
            {Object.entries(groupedSuggestions).map(([type, items]) => {
              const Icon = typeIcons[type] || FileText;
              const label = typeLabels[type] || type;

              return (
                <CommandGroup key={type} heading={label}>
                  {items.map((suggestion) => (
                    <CommandItem
                      key={`${suggestion.type}-${suggestion.id}`}
                      value={`${suggestion.title}-${suggestion.id}`}
                      onSelect={() => handleSelect(suggestion)}
                      className="flex items-center gap-3 py-3"
                    >
                      {suggestion.type === 'users' ? (
                        <Avatar className="h-8 w-8">
                          <AvatarImage src={suggestion.imageUrl || undefined} />
                          <AvatarFallback>
                            {suggestion.title.split(' ').map(n => n[0]).join('').slice(0, 2)}
                          </AvatarFallback>
                        </Avatar>
                      ) : suggestion.imageUrl ? (
                        <img
                          src={suggestion.imageUrl}
                          alt=""
                          className="h-8 w-8 rounded object-cover"
                        />
                      ) : (
                        <div className="h-8 w-8 rounded bg-muted flex items-center justify-center">
                          <Icon className="h-4 w-4 text-muted-foreground" />
                        </div>
                      )}
                      <div className="flex-1 min-w-0">
                        <p className="font-medium truncate">{suggestion.title}</p>
                        {suggestion.subtitle && (
                          <p className="text-sm text-muted-foreground truncate">
                            {suggestion.subtitle}
                          </p>
                        )}
                      </div>
                    </CommandItem>
                  ))}
                </CommandGroup>
              );
            })}

            <CommandSeparator />

            <CommandGroup>
              <CommandItem
                onSelect={handleViewAll}
                className="flex items-center justify-between py-3"
              >
                <span className="text-muted-foreground">
                  View all results for "{query}"
                </span>
                <ArrowRight className="h-4 w-4 text-muted-foreground" />
              </CommandItem>
              <CommandItem
                onSelect={() => {
                  onOpenChange(false);
                  router.push(`/search?q=${encodeURIComponent(query.trim())}&ai=true`);
                  setQuery('');
                }}
                className="flex items-center justify-between py-3"
              >
                <div className="flex items-center gap-2">
                  <Bot className="h-4 w-4 text-purple-500" />
                  <span className="text-muted-foreground">
                    Ask AI about "{query}"
                  </span>
                </div>
                <Badge variant="secondary" className="text-xs">
                  <Sparkles className="h-3 w-3 mr-1" />
                  AI
                </Badge>
              </CommandItem>
            </CommandGroup>
          </>
        )}
      </CommandList>
    </CommandDialog>
  );
}
