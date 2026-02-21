'use client';

import { useRouter, useSearchParams } from 'next/navigation';
import { useCallback } from 'react';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { X } from 'lucide-react';
import { useNewsChannels, usePopularTags, useNewsSpaces } from '../api';
import { Skeleton } from '@/components/ui/skeleton';

export function NewsFilters() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const channelSlug = searchParams.get('channel') || '';
  const spaceSlug = searchParams.get('space') || '';
  const tagSlug = searchParams.get('tag') || '';
  const sort = searchParams.get('sort') || 'latest';

  const { data: channels, isLoading: channelsLoading } = useNewsChannels();
  const { data: spaces, isLoading: spacesLoading } = useNewsSpaces();
  const { data: tags, isLoading: tagsLoading } = usePopularTags(8);

  const updateFilter = useCallback(
    (key: string, value: string) => {
      const params = new URLSearchParams(searchParams.toString());
      if (value) {
        params.set(key, value);
      } else {
        params.delete(key);
      }
      router.push(`/news?${params.toString()}`, { scroll: false });
    },
    [router, searchParams]
  );

  const clearFilters = useCallback(() => {
    router.push('/news', { scroll: false });
  }, [router]);

  const hasActiveFilters = channelSlug || spaceSlug || tagSlug;

  return (
    <div className="space-y-4 mb-8">
      {/* Dropdowns Row */}
      <div className="flex flex-wrap items-center gap-3">
        {/* Channel Dropdown */}
        {channelsLoading ? (
          <Skeleton className="h-10 w-40" />
        ) : (
          <Select value={channelSlug || 'all'} onValueChange={(v) => updateFilter('channel', v === 'all' ? '' : v)}>
            <SelectTrigger className="w-40">
              <SelectValue placeholder="All Channels" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Channels</SelectItem>
              {channels?.map((channel) => (
                <SelectItem key={channel.id} value={channel.slug}>
                  {channel.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}

        {/* Space Dropdown */}
        {spacesLoading ? (
          <Skeleton className="h-10 w-40" />
        ) : (
          <Select value={spaceSlug || 'all'} onValueChange={(v) => updateFilter('space', v === 'all' ? '' : v)}>
            <SelectTrigger className="w-40">
              <SelectValue placeholder="All Spaces" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Spaces</SelectItem>
              {spaces?.map((space) => (
                <SelectItem key={space.id} value={space.slug}>
                  {space.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}

        {/* Sort Dropdown */}
        <Select value={sort} onValueChange={(v) => updateFilter('sort', v)}>
          <SelectTrigger className="w-32">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="latest">Latest</SelectItem>
            <SelectItem value="popular">Popular</SelectItem>
          </SelectContent>
        </Select>

        {/* Clear Filters */}
        {hasActiveFilters && (
          <Button variant="ghost" size="sm" onClick={clearFilters} className="gap-1">
            <X className="h-4 w-4" />
            Clear
          </Button>
        )}
      </div>

      {/* Tag Pills Row */}
      {tagsLoading ? (
        <div className="flex flex-wrap gap-2">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-7 w-20 rounded-full" />
          ))}
        </div>
      ) : tags && tags.length > 0 ? (
        <div className="flex flex-wrap gap-2">
          {tags.map((tag) => (
            <Badge
              key={tag.id}
              variant={tagSlug === tag.slug ? 'default' : 'outline'}
              className="cursor-pointer hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              onClick={() => updateFilter('tag', tagSlug === tag.slug ? '' : tag.slug)}
            >
              {tag.name}
            </Badge>
          ))}
        </div>
      ) : null}
    </div>
  );
}
