import { usePaginatedQuery, useApiQuery } from '@/lib/api-hooks';

export interface NewsArticle {
  id: string;
  title: string;
  slug: string;
  teaserText?: string;
  teaserImageUrl?: string;
  status: 'Draft' | 'Scheduled' | 'Published' | 'Archived';
  publishedAt?: string;
  isPinned: boolean;
  channel?: {
    id: string;
    name: string;
    slug: string;
    color?: string;
  };
  author: {
    id: string;
    displayName: string;
    avatarUrl?: string;
  };
  displayAuthor: {
    id: string;
    displayName: string;
    avatarUrl?: string;
  };
  tags: Array<{ id: string; name: string; slug: string }>;
  commentCount: number;
  likeCount: number;
  viewCount: number;
}

export interface NewsFilters {
  spaceSlug?: string;
  channelSlug?: string;
  tagSlug?: string;
  status?: string;
  isPinned?: boolean;
  search?: string;
  cursor?: string;
  size?: number;
}

export function useNewsFeed(filters: NewsFilters = {}) {
  const params = new URLSearchParams();
  if (filters.spaceSlug) params.set('spaceSlug', filters.spaceSlug);
  if (filters.channelSlug) params.set('channelSlug', filters.channelSlug);
  if (filters.tagSlug) params.set('tagSlug', filters.tagSlug);
  if (filters.status) params.set('status', filters.status);
  if (filters.isPinned !== undefined) params.set('isPinned', String(filters.isPinned));
  if (filters.search) params.set('search', filters.search);
  if (filters.cursor) params.set('cursor', filters.cursor);
  if (filters.size) params.set('size', String(filters.size));

  const queryString = params.toString();
  const url = `/api/v1/news${queryString ? `?${queryString}` : ''}`;

  return usePaginatedQuery<NewsArticle>(
    ['news', 'feed', filters],
    url,
    undefined,
    {
      staleTime: 30 * 1000, // 30 seconds
    }
  );
}

export function useNewsArticle(spaceSlug: string, articleSlug: string) {
  return useApiQuery<NewsArticle>(
    ['news', 'article', spaceSlug, articleSlug],
    `/api/v1/news/${spaceSlug}/${articleSlug}`,
    {
      enabled: !!spaceSlug && !!articleSlug,
    }
  );
}
