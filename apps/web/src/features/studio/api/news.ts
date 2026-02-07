import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { WidgetBlock } from '@/features/pages/widgets';

export type ArticleStatus = 'Draft' | 'Published' | 'Scheduled' | 'Archived';

export interface StudioNewsArticle {
  id: string;
  title: string;
  slug: string;
  teaserText?: string;
  teaserImageUrl?: string;
  content?: WidgetBlock[];
  status: ArticleStatus;
  publishedAt?: string;
  scheduledAt?: string;
  createdAt: string;
  updatedAt: string;
  isPinned: boolean;
  allowComments: boolean;
  viewCount: number;
  likeCount: number;
  commentCount: number;
  channelId?: string;
  channel?: {
    id: string;
    name: string;
    slug: string;
  };
  spaceId?: string;
  space?: {
    id: string;
    name: string;
    slug: string;
  };
  authorId: string;
  author: {
    id: string;
    firstName: string;
    lastName: string;
    displayName: string;
    avatarUrl?: string;
  };
  displayAuthorId?: string;
  displayAuthor?: {
    id: string;
    firstName: string;
    lastName: string;
    displayName: string;
    avatarUrl?: string;
  };
  tags: Array<{ id: string; name: string; slug: string }>;
}

interface NewsListResponse {
  items: StudioNewsArticle[];
  totalCount: number;
  hasNextPage: boolean;
  nextCursor?: string;
}

export interface NewsListFilters {
  status?: ArticleStatus;
  channelId?: string;
  search?: string;
}

// List Articles
export function useStudioNewsList(filters: NewsListFilters = {}) {
  return useInfiniteQuery({
    queryKey: ['studio-news', filters],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (filters.status) params.set('status', filters.status);
      if (filters.channelId) params.set('channelId', filters.channelId);
      if (filters.search) params.set('search', filters.search);
      if (pageParam) params.set('cursor', pageParam);
      params.set('limit', '20');

      return apiClient.get<NewsListResponse>(`/studio/news?${params.toString()}`);
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.hasNextPage ? lastPage.nextCursor : undefined,
  });
}

// Get Single Article
export function useStudioNewsArticle(id: string) {
  return useQuery({
    queryKey: ['studio-news-article', id],
    queryFn: async () => {
      return apiClient.get<StudioNewsArticle>(`/studio/news/${id}`);
    },
    enabled: !!id && id !== 'new',
  });
}

// Create Article
export interface CreateArticleInput {
  title: string;
  slug?: string;
  teaserText?: string;
  teaserImageUrl?: string;
  content?: WidgetBlock[];
  channelId?: string;
  spaceId?: string;
  displayAuthorId?: string;
  tags?: string[];
  isPinned?: boolean;
  allowComments?: boolean;
}

export function useCreateArticle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: CreateArticleInput) => {
      return apiClient.post<StudioNewsArticle>('/studio/news', input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-news'] });
    },
  });
}

// Update Article
export interface UpdateArticleInput extends Partial<CreateArticleInput> {
  id: string;
  status?: ArticleStatus;
  scheduledAt?: string;
}

export function useUpdateArticle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, ...input }: UpdateArticleInput) => {
      return apiClient.put<StudioNewsArticle>(`/studio/news/${id}`, input);
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['studio-news'] });
      queryClient.setQueryData(['studio-news-article', data.id], data);
    },
  });
}

// Publish Article
export function usePublishArticle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      return apiClient.post<StudioNewsArticle>(`/studio/news/${id}/publish`);
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['studio-news'] });
      queryClient.setQueryData(['studio-news-article', data.id], data);
    },
  });
}

// Schedule Article
export function useScheduleArticle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, scheduledAt }: { id: string; scheduledAt: string }) => {
      return apiClient.post<StudioNewsArticle>(`/studio/news/${id}/schedule`, { scheduledAt });
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['studio-news'] });
      queryClient.setQueryData(['studio-news-article', data.id], data);
    },
  });
}

// Archive Article
export function useArchiveArticle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      return apiClient.post<StudioNewsArticle>(`/studio/news/${id}/archive`);
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['studio-news'] });
      queryClient.setQueryData(['studio-news-article', data.id], data);
    },
  });
}

// Delete Article
export function useDeleteArticle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/studio/news/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-news'] });
    },
  });
}

// Bulk Actions
export function useBulkDeleteArticles() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/studio/news/bulk-delete', { ids });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-news'] });
    },
  });
}

export function useBulkArchiveArticles() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/studio/news/bulk-archive', { ids });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-news'] });
    },
  });
}

// Tags autocomplete
export function useTagsAutocomplete(search: string) {
  return useQuery({
    queryKey: ['tags-autocomplete', search],
    queryFn: async () => {
      return apiClient.get<Array<{ id: string; name: string; slug: string }>>(
        `/news/tags?search=${encodeURIComponent(search)}&limit=10`
      );
    },
    enabled: search.length >= 2,
    staleTime: 30 * 1000,
  });
}

// Channels list
export function useStudioChannels() {
  return useQuery({
    queryKey: ['studio-channels'],
    queryFn: async () => {
      return apiClient.get<Array<{ id: string; name: string; slug: string }>>('/news/channels');
    },
    staleTime: 5 * 60 * 1000,
  });
}

// Spaces list
export function useStudioSpaces() {
  return useQuery({
    queryKey: ['studio-spaces'],
    queryFn: async () => {
      return apiClient.get<Array<{ id: string; name: string; slug: string }>>('/spaces?limit=100');
    },
    staleTime: 5 * 60 * 1000,
    select: (data) => (data as { items?: Array<{ id: string; name: string; slug: string }> }).items || data,
  });
}

// User search for ghostwriting
export interface UserSearchResult {
  id: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  avatarUrl?: string;
  email: string;
}

export function useUserSearch(search: string) {
  return useQuery({
    queryKey: ['user-search', search],
    queryFn: async () => {
      return apiClient.get<{ items: UserSearchResult[] }>(
        `/people/search?q=${encodeURIComponent(search)}&size=10`
      );
    },
    enabled: search.length >= 2,
    staleTime: 30 * 1000,
    select: (data) => data.items,
  });
}
