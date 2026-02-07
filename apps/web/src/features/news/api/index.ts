import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { WidgetBlock } from '@/features/pages/widgets';

// Types
export interface NewsChannel {
  id: string;
  name: string;
  slug: string;
  description?: string;
  color?: string;
  spaceId?: string;
}

export interface NewsTag {
  id: string;
  name: string;
  slug: string;
}

export interface Author {
  id: string;
  firstName: string;
  lastName: string;
  displayName?: string;
  avatarUrl?: string;
}

export interface NewsArticle {
  id: string;
  title: string;
  slug: string;
  teaserText?: string;
  teaserImageUrl?: string;
  content?: WidgetBlock[];
  status: 'Draft' | 'Scheduled' | 'Published' | 'Archived';
  publishedAt?: string;
  createdAt: string;
  updatedAt: string;
  isPinned: boolean;
  readingTimeMinutes?: number;
  viewCount: number;
  likeCount: number;
  commentCount: number;
  hasLiked?: boolean;
  channel: NewsChannel;
  author: Author;
  displayAuthor?: Author;
  tags: NewsTag[];
  space?: {
    id: string;
    name: string;
    slug: string;
  };
}

export interface NewsComment {
  id: string;
  content: string;
  createdAt: string;
  updatedAt: string;
  isEdited: boolean;
  isModerated: boolean;
  author: Author;
  parentId?: string;
  replies?: NewsComment[];
}

export interface NewsFilters {
  channelSlug?: string;
  spaceSlug?: string;
  tagSlug?: string;
  sort?: 'latest' | 'popular';
  isPinned?: boolean;
}

interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  hasNextPage: boolean;
  nextCursor?: string;
}

// News Feed Hook with Infinite Scroll
export function useNewsFeed(filters: NewsFilters = {}) {
  return useInfiniteQuery({
    queryKey: ['news-feed', filters],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (filters.channelSlug) params.set('channelSlug', filters.channelSlug);
      if (filters.spaceSlug) params.set('spaceSlug', filters.spaceSlug);
      if (filters.tagSlug) params.set('tagSlug', filters.tagSlug);
      if (filters.sort) params.set('sort', filters.sort);
      if (filters.isPinned !== undefined) params.set('isPinned', String(filters.isPinned));
      if (pageParam) params.set('cursor', pageParam);
      params.set('limit', '12');
      params.set('status', 'Published');

      return apiClient.get<PaginatedResponse<NewsArticle>>(`/news/articles?${params.toString()}`);
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.hasNextPage ? lastPage.nextCursor : undefined,
    staleTime: 30 * 1000,
  });
}

// Pinned Articles Hook
export function usePinnedArticles(spaceSlug?: string) {
  return useQuery({
    queryKey: ['news-pinned', spaceSlug],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.set('isPinned', 'true');
      params.set('status', 'Published');
      params.set('limit', '10');
      if (spaceSlug) params.set('spaceSlug', spaceSlug);

      return apiClient.get<PaginatedResponse<NewsArticle>>(`/news/articles?${params.toString()}`);
    },
    staleTime: 60 * 1000,
  });
}

// Single Article Hook
export function useNewsArticle(slug: string) {
  return useQuery({
    queryKey: ['news-article', slug],
    queryFn: async () => {
      return apiClient.get<NewsArticle>(`/news/articles/${slug}`);
    },
    enabled: !!slug,
  });
}

// Related Articles Hook
export function useRelatedArticles(channelSlug: string, excludeArticleId: string) {
  return useQuery({
    queryKey: ['news-related', channelSlug, excludeArticleId],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.set('channelSlug', channelSlug);
      params.set('status', 'Published');
      params.set('limit', '3');
      params.set('excludeId', excludeArticleId);

      return apiClient.get<PaginatedResponse<NewsArticle>>(`/news/articles?${params.toString()}`);
    },
    enabled: !!channelSlug && !!excludeArticleId,
    staleTime: 60 * 1000,
  });
}

// Comments Hooks
export function useComments(articleId: string) {
  return useInfiniteQuery({
    queryKey: ['news-comments', articleId],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (pageParam) params.set('cursor', pageParam);
      params.set('limit', '20');

      return apiClient.get<PaginatedResponse<NewsComment>>(`/news/articles/${articleId}/comments?${params.toString()}`);
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.hasNextPage ? lastPage.nextCursor : undefined,
    enabled: !!articleId,
  });
}

interface AddCommentInput {
  articleId: string;
  content: string;
  parentId?: string;
}

export function useAddComment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: AddCommentInput) => {
      return apiClient.post<NewsComment>(`/news/articles/${input.articleId}/comments`, {
        content: input.content,
        parentId: input.parentId,
      });
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['news-comments', variables.articleId] });
      queryClient.invalidateQueries({ queryKey: ['news-article'] });
    },
  });
}

interface UpdateCommentInput {
  articleId: string;
  commentId: string;
  content: string;
}

export function useUpdateComment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: UpdateCommentInput) => {
      return apiClient.put<NewsComment>(
        `/news/articles/${input.articleId}/comments/${input.commentId}`,
        { content: input.content }
      );
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['news-comments', variables.articleId] });
    },
  });
}

interface DeleteCommentInput {
  articleId: string;
  commentId: string;
}

export function useDeleteComment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: DeleteCommentInput) => {
      await apiClient.delete(`/news/articles/${input.articleId}/comments/${input.commentId}`);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['news-comments', variables.articleId] });
      queryClient.invalidateQueries({ queryKey: ['news-article'] });
    },
  });
}

// Reaction/Like Hook with Optimistic Update
export function useToggleReaction(articleId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async () => {
      return apiClient.post<{ liked: boolean; likeCount: number }>(
        `/news/articles/${articleId}/reactions/toggle`
      );
    },
    onMutate: async () => {
      // Cancel any outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['news-article', articleId] });

      // Snapshot the previous value
      const previousArticle = queryClient.getQueryData<NewsArticle>(['news-article', articleId]);

      // Optimistically update
      if (previousArticle) {
        queryClient.setQueryData<NewsArticle>(['news-article', articleId], {
          ...previousArticle,
          hasLiked: !previousArticle.hasLiked,
          likeCount: previousArticle.hasLiked
            ? previousArticle.likeCount - 1
            : previousArticle.likeCount + 1,
        });
      }

      return { previousArticle };
    },
    onError: (_, __, context) => {
      // Roll back on error
      if (context?.previousArticle) {
        queryClient.setQueryData(['news-article', articleId], context.previousArticle);
      }
    },
    onSettled: () => {
      // Always refetch after error or success
      queryClient.invalidateQueries({ queryKey: ['news-article', articleId] });
    },
  });
}

// Channels Hook
export function useNewsChannels() {
  return useQuery({
    queryKey: ['news-channels'],
    queryFn: async () => {
      return apiClient.get<NewsChannel[]>('/news/channels');
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

// Tags Search Hook
export function useSearchTags(search: string) {
  return useQuery({
    queryKey: ['news-tags', search],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (search) params.set('search', search);
      params.set('limit', '20');

      return apiClient.get<NewsTag[]>(`/news/tags?${params.toString()}`);
    },
    enabled: search.length >= 2,
    staleTime: 60 * 1000,
  });
}

// Popular Tags Hook
export function usePopularTags(limit = 10) {
  return useQuery({
    queryKey: ['news-tags-popular', limit],
    queryFn: async () => {
      return apiClient.get<NewsTag[]>(`/news/tags?sort=popular&limit=${limit}`);
    },
    staleTime: 5 * 60 * 1000,
  });
}

// Spaces Hook for Filter
export function useNewsSpaces() {
  return useQuery({
    queryKey: ['news-spaces'],
    queryFn: async () => {
      return apiClient.get<Array<{ id: string; name: string; slug: string }>>('/spaces?hasNews=true');
    },
    staleTime: 5 * 60 * 1000,
  });
}
