import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';

export type CommentStatus = 'Pending' | 'Approved' | 'Flagged' | 'Rejected';

export interface StudioComment {
  id: string;
  body: string;
  status: CommentStatus;
  createdAt: string;
  updatedAt: string;
  author: {
    id: string;
    displayName: string;
    avatarUrl?: string;
    email: string;
  };
  article: {
    id: string;
    title: string;
    slug: string;
  };
  parentId?: string;
  replyCount: number;
  flagReason?: string;
}

interface CommentsListResponse {
  items: StudioComment[];
  totalCount: number;
  hasNextPage: boolean;
  nextCursor?: string;
  statusCounts: {
    pending: number;
    approved: number;
    flagged: number;
    rejected: number;
  };
}

export interface CommentsListFilters {
  status?: CommentStatus;
  search?: string;
  articleId?: string;
}

// List comments
export function useStudioComments(filters: CommentsListFilters = {}) {
  return useInfiniteQuery({
    queryKey: ['studio-comments', filters],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (filters.status) params.set('status', filters.status);
      if (filters.search) params.set('search', filters.search);
      if (filters.articleId) params.set('articleId', filters.articleId);
      if (pageParam) params.set('cursor', pageParam);
      params.set('limit', '20');

      return apiClient.get<CommentsListResponse>(`/studio/comments?${params.toString()}`);
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.hasNextPage ? lastPage.nextCursor : undefined,
  });
}

// Approve comment
export function useApproveComment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      return apiClient.post<StudioComment>(`/studio/comments/${id}/approve`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-comments'] });
    },
  });
}

// Reject comment
export function useRejectComment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      return apiClient.post<StudioComment>(`/studio/comments/${id}/reject`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-comments'] });
    },
  });
}

// Flag comment
export function useFlagComment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, reason }: { id: string; reason: string }) => {
      return apiClient.post<StudioComment>(`/studio/comments/${id}/flag`, { reason });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-comments'] });
    },
  });
}

// Delete comment
export function useDeleteComment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/studio/comments/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-comments'] });
    },
  });
}

// Bulk actions
export function useBulkApproveComments() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/studio/comments/bulk-approve', { ids });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-comments'] });
    },
  });
}

export function useBulkRejectComments() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/studio/comments/bulk-reject', { ids });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-comments'] });
    },
  });
}

export function useBulkDeleteComments() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (ids: string[]) => {
      await apiClient.post('/studio/comments/bulk-delete', { ids });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-comments'] });
    },
  });
}
